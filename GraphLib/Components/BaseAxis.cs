﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Základní třída pro grafické prvky reprezentující osu: velikost, čas
    /// </summary>
    /// <typeparam name="TTick">Datový typ údaje Pozice na ose (DateTime, Decimal, Int32)</typeparam>
    /// <typeparam name="TSize">Datový typ údaje Vzdálenost mezi dvěma pozicemi na ose (TimeSpan, Decimal, Int32)</typeparam>
    /// <typeparam name="TValue">Datový typ intervalu, který zahrnuje oba typy TTick, TSize</typeparam>
    public abstract class BaseAxis<TTick, TSize, TValue> : InteractiveObject, IInteractiveItem
        where TValue : BaseRange<TTick, TSize>
    {
        #region Konstruktory
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public BaseAxis(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public BaseAxis()
        {
            this.Is.MouseDragMove = true;
            this.Is.MouseMoveOver = true;
            this.PrepareInitialValues();
            this.PrepareVisual();
            this.PrepareArrangement();
            this.PrepareAfter();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Orientation.ToString() + " " + base.ToString();
        }
        /// <summary>
        /// Připraví výchozí data do this._Value, _ValueHelper, _ScaleHelper
        /// </summary>
        private void PrepareInitialValues()
        {
            this._Value = this.InitialValue;                // nebudeme dávat : GetValue(default(TTick), default(TTick)), potomek může mít jiný názor
            this._ValueHelper = this.GetValue(default(TTick), default(TTick));
            this._ScaleHelper = new DecimalNRange();
        }
        /// <summary>
        /// Pomocná instance pro interval hodnot (typicky TimeRange nebo SizeRange), pro provádění výpočtů na ose
        /// </summary>
        private TValue _ValueHelper;
        /// <summary>
        /// Pomocná instance pro interval SizeRange class, pro přepočty měřítka na ose
        /// </summary>
        private DecimalNRange _ScaleHelper;
        /// <summary>
        /// Metoda volaná na konci konstruktoru, kdy jsou již všechny údaje v BaseAxis připravené.
        /// Potomek si připraví svoje data nad rámec BaseRange. Volání bázové metody není nutné.
        /// </summary>
        protected virtual void PrepareAfter() { }
        /// <summary>
        /// Připraví vzhled pro ToolTip, volá se před zobrazením Tooltipu.
        /// Je voláno poté, kdy v e.ToolTipData.InfoText jsou připravena data.
        /// Bázová metoda nastavuje: ShapeType = TooltipShapeType.Rectangle; AnimationType = TooltipAnimationType.Instant.
        /// Potomek může mít jiný názor.
        /// </summary>
        /// <param name="e"></param>
        public override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            e.ToolTipData.ShapeType = TooltipShapeType.Rectangle;
            e.ToolTipData.AnimationType = TooltipAnimationType.Instant;
        }
        /// <summary>
        /// Připraví a vrátí text do ToolTipu pro danou hodnotu osy.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual string PrepareToolTipText(TTick value)
        {
            string text = value.ToString();

            string segmentText = this.GetSegmentsToolTip(value);
            if (segmentText != null)
                text += segmentText;

            return value.ToString();
        }
        #endregion
        #region Vizuální properties
        /// <summary>
        /// Připraví vizuální vlastnosti.
        /// Pozor, je to virtuální třída volaná z konstruktoru, tedy před provedením konstruktoru potomků.
        /// Potomek tedy nemá připraveny svoje data.
        /// Potomek musí volat base:PrepareVisual();
        /// </summary>
        protected virtual void PrepareVisual()
        {
            this.BackColor3DEffect = 0.15f;
        }
        /// <summary>
        /// 3D effect pro BackColor
        /// </summary>
        public float BackColor3DEffect { get; set; }
        /// <summary>
        /// Defaultní barva pozadí.
        /// </summary>
        protected override Color BackColorDefault { get { return Skin.Axis.BackColor; } }
        #endregion
        #region Orientace osy, a její podpora (PixelRelativeRange, PixelFirst, PixelSize, PixelLast, ...)
        /// <summary>
        /// Orientace osy. Pokud zadaná orientace nebude odpovídat souřadnicím, bude upravena.
        /// Vhodný postup pro zadání: nastavte souřadnice (alespoň poměr velikosti) a poté vložte odpovídající orientaci.
        /// Orientace nese i informace o umístění ticků a o směru osy (zhora dolů/zdola nahoru).
        /// </summary>
        public AxisOrientation Orientation
        {
            get { return this.OrientationCurrent; }
            set { this.OrientationUser = value; this.DetectOrientation(ProcessAction.ChangeAll, EventSourceType.OrientationChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Aktuální platná orientace osy detekovaná podle <see cref="OrientationUser"/> a souřadnic, v metodě <see cref="DetectOrientation(ProcessAction, EventSourceType)"/>.
        /// </summary>
        protected AxisOrientation OrientationCurrent { get; set; }
        /// <summary>
        /// Orientace požadovaná uživatelem, bez ohledu na velikost. Pokud to velikost osy dovolí, bude použita tato orientace.
        /// </summary>
        protected AxisOrientation? OrientationUser { get; set; }
        /// <summary>
        /// WinForm orientace (Horizontal/Vertical) odvozená od <see cref="Orientation"/>.
        /// Používá se při kreslení gradientů a tak.
        /// </summary>
        protected System.Windows.Forms.Orientation OrientationDraw
        {
            get
            {
                switch (this.Orientation)
                {
                    case AxisOrientation.Top:
                    case AxisOrientation.Bottom:
                        return System.Windows.Forms.Orientation.Horizontal;
                    case AxisOrientation.LeftUp:
                    case AxisOrientation.LeftDown:
                    case AxisOrientation.RightUp:
                    case AxisOrientation.RightDown:
                        return System.Windows.Forms.Orientation.Vertical;
                }
                return System.Windows.Forms.Orientation.Horizontal;
            }
        }
        /// <summary>
        /// Rozsah pixelů z Bounds, odpovídající aktuální orientaci. 
        /// Hodnota DecimalNRange.Begin obsahuje pixel, kde je vykreslena hodnota osy Begin (X, Top, Bottom).
        /// Hodnota DecimalNRange.End obsahuje pixel, kde je pozice osy End (Right, Bottom, Top);
        /// Hodnota DecimalNRange.Size = počet pixelů osy (kladné nebo záporné číslo).
        /// </summary>
        protected DecimalNRange PixelRelativeRange
        {
            get
            {
                Rectangle b = this.Bounds;
                switch (this.OrientationCurrent)
                {
                    case AxisOrientation.Top: return new DecimalNRange(b.Left, b.Right);
                    case AxisOrientation.LeftUp: return new DecimalNRange(b.Bottom, b.Top);
                    case AxisOrientation.LeftDown: return new DecimalNRange(b.Top, b.Bottom);
                    case AxisOrientation.RightUp: return new DecimalNRange(b.Bottom, b.Top);
                    case AxisOrientation.RightDown: return new DecimalNRange(b.Top, b.Bottom);
                    case AxisOrientation.Bottom: 
                    default:
                        return new DecimalNRange(b.Left, b.Right);
                }
            }
        }
        /// <summary>
        /// Current size in pixel (as decimal), by this.Orientation returns: this.VisibleAbsoluteBounds.Width or Height
        /// </summary>
        protected decimal PixelSize { get { return Math.Abs(this.PixelRelativeRange.Size.Value); } }
        /// <summary>
        /// Current first pixel (as decimal), in relative value, by this.Orientation returns: this.VisibleRelativeBounds.X or this.VisibleRelativeBounds.Y or this.VisibleRelativeBounds.Bottom-1
        /// </summary>
        protected decimal PixelFirst { get { return (this.PixelRelativeRange.Begin.Value); } }
        /// <summary>
        /// Current last pixel (as decimal), in relative value, by this.Orientation returns: this.VisibleRelativeBounds.Right or this.VisibleRelativeBounds.Top or this.VisibleRelativeBounds.Bottom
        /// </summary>
        protected decimal PixelLast { get { return (this.PixelRelativeRange.End.Value); } }
        /// <summary>
        /// Returns a distance (in Pixels) from PixelFirst to relative point, by OrientationCurrent.
        /// RelativePoint is relative to this.owner: for example point {0,0} is on Top/Left on this Host.
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <returns></returns>
        protected decimal PixelDistanceFromRelativePoint(Point relativePoint)
        {
            Rectangle bounds = this.Bounds;
            switch (this.OrientationCurrent)
            {
                case AxisOrientation.Top:
                case AxisOrientation.Bottom:
                    return relativePoint.X - bounds.X;               // Osa jde zleva doprava: na souřadnici bounds.X je pixel == 0, vyšší hodnota relativePoint.X obsahuje větší pixel
                case AxisOrientation.LeftUp:
                case AxisOrientation.RightUp:
                    return bounds.Bottom - relativePoint.Y;          // Osa jde zdola nahoru: na souřadnici bounds.Bottom je pixel == 0, menší hodnota relativePoint.Y obsahuje větší pixel
                case AxisOrientation.LeftDown:
                case AxisOrientation.RightDown:
                    return relativePoint.Y - bounds.Y;               // Osa jde zhora dolů: na souřadnici bounds.Y je pixel 0, vyšší hodnota relativePoint.Y obsahuje větší pixel
            }
            return 0m;
        }
        /// <summary>
        /// Returns a distance (in Pixels) from PixelFirst to relative point, by OrientationCurrent.
        /// RelativePoint is relative to this.VisibleRelativeBounds: for example point {0,0} is on Top/Left on this Axis.
        /// </summary>
        /// <param name="localPoint"></param>
        /// <returns></returns>
        protected decimal PixelDistanceFromLocalPoint(Point localPoint)
        {
            Rectangle bounds = this.Bounds;
            switch (this.OrientationCurrent)
            {
                case AxisOrientation.Top:
                case AxisOrientation.Bottom:
                    return localPoint.X;                             // Osa jde zleva doprava: na souřadnici bounds.X je pixel == 0, vyšší hodnota relativePoint.X obsahuje větší pixel
                case AxisOrientation.LeftUp:
                case AxisOrientation.RightUp:
                    return bounds.Height - localPoint.Y;             // Osa jde zdola nahoru: na souřadnici bounds.Bottom je pixel == 0, menší hodnota relativePoint.Y obsahuje větší pixel
                case AxisOrientation.LeftDown:
                case AxisOrientation.RightDown:
                    return localPoint.Y;                             // Osa jde zhora dolů: na souřadnici bounds.Y je pixel 0, vyšší hodnota relativePoint.Y obsahuje větší pixel
            }
            return 0m;
        }
        /// <summary>
        /// Returns a point relative coordinate (in Pixel) for pixel distance from Axis.Begin.
        /// Returns active coordinate = X or Y, by OrientationCurrent.
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        protected decimal PixelPointFromDistance(decimal distance)
        {
            Rectangle bounds = this.Bounds;
            switch (this.OrientationCurrent)
            {
                case AxisOrientation.Top:
                case AxisOrientation.Bottom:
                    return bounds.Left + distance;                   // Osa jde zleva doprava: na souřadnici bounds.X je pixel == 0, kladná distance vrací větší pixel (doprava)
                case AxisOrientation.LeftUp:
                case AxisOrientation.RightUp:
                    return bounds.Bottom - distance;                 // Osa jde zdola nahoru: na souřadnici bounds.Bottom je pixel == 0, kladná distance vrací menší pixel (nahoru)
                case AxisOrientation.LeftDown:
                case AxisOrientation.RightDown:
                    return bounds.Top + distance;                    // Osa jde zhora dolů: na souřadnici bounds.Top je pixel 0, kladná distance vrací větší pixel (dolů)
            }
            return 0m;
        }
        /// <summary>
        /// Return a shift distance in number of pixel, by OrientationCurrent, from shift in Point (on control).
        /// For orientation Top + Bottom return X, for orientation LeftDown + RightDown return Y, for orientation LeftUp + RightUp returns -Y.
        /// </summary>
        /// <param name="shift"></param>
        /// <returns></returns>
        protected int GetShiftByOrientation(Point shift)
        {
            switch (this.OrientationCurrent)
            {
                case AxisOrientation.Top:
                case AxisOrientation.Bottom: return shift.X;
                case AxisOrientation.LeftUp:
                case AxisOrientation.RightUp: return -shift.Y;
                case AxisOrientation.LeftDown:
                case AxisOrientation.RightDown: return shift.Y;
            }
            return 0;
        }
        #endregion
        #region Data: Value, ValueSilent, ValueLimit = aktuální rozsah osy. Scale, ScaleLimit = aktuální měřítko. ResizeContentMode, IsValid, Identity.
        /// <summary>
        /// Value of Axis = Visible Range of data on Axis
        /// </summary>
        public TValue Value
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.ChangeAll, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Value of Axis = Visible Range of data on Axis.
        /// Silent = does not call any events, nor Draw
        /// </summary>
        public TValue ValueSilent
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.SilentValueActions, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Current value of Axis = Visible Range of data on Axis
        /// </summary>
        private TValue _Value;
        /// <summary>
        /// Maximum limits of Value for Axis.Value (minima Begin and maxima End).
        /// Empty edge is not limiting for this end. Empty value (or null) in ValueRange is not limiting for booth ends (Axis can stretch to infinity).
        /// </summary>
        public TValue ValueLimit
        {
            get { return this._ValueLimit; }
            set { this.SetValueLimit(value, ProcessAction.ChangeAll, EventSourceType.ValueRangeChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Current ValueLimit value
        /// </summary>
        private TValue _ValueLimit;
        /// <summary>
        /// Aktuální měřítko.
        /// Měřítko je kladná hodnota, která vyjadřuje poměr = počet logických jednotek osy na jeden zobrazený pixel.
        /// Logická jednotka na ose je decimal číslo, které vrací metoda <see cref="GetAxisUnits(TSize)"/>.
        /// Například časová osa vrací celkový počet sekund z dodaného časového úseku <see cref="TimeSpan"/>, což je <typeparamref name="TSize"/> na časové ose.
        /// Měřítko lze setovat, pouze kladnou hodnotu. Z dodaného měřítka je následně určena hodnota <see cref="Value"/> na této ose.
        /// </summary>
        public decimal Scale
        {
            get { return this._Scale; }
            set { this.SetScale(value, ProcessAction.ChangeAll, EventSourceType.ValueScaleChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Current Scale value = number of axis logical units per one visual pixel.
        /// </summary>
        private decimal _Scale { get { return this.__Scale; } set { this.__Scale = value;  this._LastTickPositionDict = null; } }
        private decimal __Scale;
        /// <summary>
        /// Maximum limits for Scale value.
        /// Scale can not be outside this Limit, when is specified.
        /// When ScaleRange is null or empty, then Scale can be any positive number.
        /// When ScaleRange allow non-positive numbers, then Scale still must be a positive.
        /// ScaleRange can contains booth values (Begin and End), or one value (then Scale will be limited only with filled value).
        /// Setting ScaleRange can cause change in Scale value (when Scale not comply new ScaleRange), and then there is a change in the value of Value on Axis. 
        /// </summary>
        public DecimalNRange ScaleLimit
        {
            get { return this._ScaleLimit; }
            set { this.SetScaleLimit(value, ProcessAction.ChangeAll, EventSourceType.ValueRangeChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Current ScaleLimit value
        /// </summary>
        private DecimalNRange _ScaleLimit;
        /// <summary>
        /// Režim změny obsahu po změně rozměru.
        /// Výchozí režim je ChangeValueEnd.
        /// </summary>
        public virtual AxisResizeContentMode ResizeContentMode
        {
            get { return this._ResizeContentMode; }
            set { this._ResizeContentMode = value; }
        }
        /// <summary>
        /// Režim změny obsahu po změně rozměru.
        /// </summary>
        protected AxisResizeContentMode _ResizeContentMode = AxisResizeContentMode.ChangeValueEnd;
        /// <summary>
        /// Možnosti uživatele změnit zobrazený rozsah anebo měřítko
        /// </summary>
        public virtual AxisInteractiveChangeMode InteractiveChangeMode
        {
            get { return this._InteractiveChangeMode; }
            set { this._InteractiveChangeMode = value; }
        }
        /// <summary>
        /// Možnosti uživatele změnit zobrazený rozsah anebo měřítko
        /// </summary>
        protected AxisInteractiveChangeMode _InteractiveChangeMode = AxisInteractiveChangeMode.All;
        /// <summary>
        /// Contains true, when is valid all: Value and Scale and Visual
        /// </summary>
        public bool IsValid { get { return this.IsAxisValid; } }
        /// <summary>
        /// Identity of axis
        /// </summary>
        public virtual string Identity
        {
            get
            {
                string identity = "";
                if (this.IsValid)
                    identity = this.PixelSize.ToString() + ":" + this._Value.ToString();
                return identity;
            }
        }
        /// <summary>
        /// Contains true, when this.Value is not null, is filled and its size is positive (this.GetAxisUnits(this._Value.Size) returns not null positive value).
        /// </summary>
        protected bool IsValueValid
        {
            get
            {
                if (this._Value == null || !this._Value.IsFilled) return false;
                decimal? units = this.GetAxisUnits(this._Value.Size);
                return (units.HasValue && units.Value > 0m);
            }
        }
        /// <summary>
        /// Contains true, when this.Scale is a positive number
        /// </summary>
        protected bool IsScaleValid { get { return (this._Scale > 0m); } }
        /// <summary>
        /// Contains true, when this.Bounds has positive Width and Height
        /// </summary>
        protected bool IsBoundsValid { get { return (this.Bounds.Width > 0 && this.Bounds.Height > 0); } }
        /// <summary>
        /// Contains true, when this.PixelSize is at least (this.PixelSizeMinimum) pixel (default = 20)
        /// </summary>
        protected bool IsVisualValid { get { return (this.PixelSize >= this.PixelSizeMinimum); } }
        /// <summary>
        /// Contains true, when is valid all: Value and Scale and Visual (this.IsValueValid AND this.IsScaleValid AND this.IsVisualValid)
        /// </summary>
        protected bool IsAxisValid { get { return this.IsValueValid && this.IsScaleValid && this.IsVisualValid; } }
        /// <summary>
        /// Minimum size of Axis (in pixels) for Valid Axis. Default = 20m
        /// </summary>
        protected virtual decimal PixelSizeMinimum { get { return 20m; } }
        #endregion
        #region Různé přepočtové metody mezi údaji typu: Value, Size, Pixel, Zoom, Ratio, Shift, Scale
        /// <summary>
        /// Vrátí pozici souřadnice pixelu pro danou logickou hodnotu, bez zaokrouhlení.
        /// Vrátí hodnotu pixelu 0 pro daný tick rovný počátku osy = this.Value.Begin.
        /// Může vrátit null, pokud výpočet this.GetAxisUnits(tick - this.Value) vrátí null.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        public decimal? CalculatePositionLocalForTick(TTick tick)
        {
            decimal? pixels = this._CalculatePixelLocalForTick(tick);
            return pixels;
        }
        /// <summary>
        /// Vrátí vizuální souřadnici pixelu pro danou logickou hodnotu.
        /// Vrátí hodnotu pixelu 0 pro daný tick rovný počátku osy = this.Value.Begin.
        /// Může vrátit null, pokud výpočet this.GetAxisUnits(tick - this.Value) vrátí null.
        /// </summary>
        /// <param name="tick">Logická hodnota</param>
        /// <returns></returns>
        public Int32? CalculatePixelLocalForTick(TTick tick)
        {
            decimal? pixels = this._CalculatePixelLocalForTick(tick);
            return (pixels.HasValue ? (Int32?)(Math.Round(pixels.Value, 0)) : (Int32?)null);
        }
        /// <summary>
        /// Vrátí relativní pozici v pixelech v relativních koordinátech (k this.Bounds) ve směru X nebo Y podle <see cref="OrientationCurrent"/>, 
        /// pro daný <typeparamref name="TTick"/> (tedy pro logickou hodnotu).
        /// Může vrátit null, pokud metoda <see cref="GetAxisUnits(TSize)"/> vrátí null.
        /// </summary>
        /// <param name="tick">Logická hodnota</param>
        /// <returns></returns>
        public Int32? CalculatePixelRelativeForTick(TTick tick)
        {
            decimal? pixels = this._CalculatePixelLocalForTick(tick);
            return (pixels.HasValue ? (Int32?)(Math.Round(this.PixelPointFromDistance(pixels.Value), 0)) : (Int32?)null);  // returns (PixelFirst + pixels.Value)
        }
        /// <summary>
        /// Vrátí vzdálenost v pixelech pro danou vzdálenost logickou <typeparamref name="TSize"/>.
        /// Může vrátit null, pokud metoda <see cref="GetAxisUnits(TSize)"/> vrátí null.
        /// </summary>
        /// <param name="size">Logická vzdálenost</param>
        /// <returns></returns>
        public Int32? CalculatePixelDistanceForTSize(TSize size)
        {
            decimal? units = this.GetAxisUnits(size);
            decimal? pixels = this.GetPixelsFromUnits(units);
            return (pixels.HasValue ? (Int32?)(Math.Round(pixels.Value, 0)) : (Int32?)null);
        }
        /// <summary>
        /// Returns a TTick (a logical value) for specified relative point (pixel value).
        /// Relative point is in sameo coordinate as this.Bounds, this is: when this.Bounds.Location = { 10, 10 } and relativePoint = { 10, 10 }, 
        /// then result value (TTick) is same as this.Value.Begin
        /// </summary>
        /// <param name="relativePoint">Relative point</param>
        /// <returns></returns>
        public TTick CalculateTickForPixelRelative(Point relativePoint)
        {
            return this.CalculateTickForPixelRelative(relativePoint, null);
        }
        /// <summary>
        /// Returns a TTick (a logical value) for specified relative point (pixel value).
        /// </summary>
        /// <param name="relativePoint">Relative point</param>
        /// <param name="roundType">Round type for round tick on current arrangement</param>
        /// <returns></returns>
        public TTick CalculateTickForPixelRelative(Point relativePoint, AxisTickType? roundType)
        {
            decimal pixels = this.PixelDistanceFromLocalPoint(relativePoint);  // Distance from Axis.Begin to relativePoint, in pixels
            decimal units = this.GetUnitsFromPixels(pixels);                   // Pixels to Units, convert by Scale
            TSize size = this.GetAxisSize(units);                              // Units to logical Size
            TTick tick = this._ValueHelper.Add(this.Value.Begin, size);        // Begin + Size = Tick
            if (roundType.HasValue && this.ArrangementCurrent != null)
                tick = this.ArrangementCurrent.RoundValueToTick(tick, roundType.Value);
            return tick;
        }
        /// <summary>
        /// Returns a logical TSize for specified distance in pixels, simply by pixel distance and current Scale of Axis.
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public TSize CalculateTSizeFromPixelDistance(decimal distance)
        {
            return this.GetAxisSize(this.GetUnitsFromPixels(distance));
        }
        /// <summary>
        /// Returns a Value from original Value, fixed point on axis (pivot tick) and target size
        /// </summary>
        /// <param name="originalValue">Old value of range</param>
        /// <param name="pivotPoint">Fixed point = Pivot point (same point on old value and on new value)</param>
        /// <param name="newSize">New Size</param>
        /// <returns></returns>
        public virtual TValue CalculateValueByZoom(TValue originalValue, TTick pivotPoint, TSize newSize)
        {
            TTick begin, end;
            this._ValueHelper.PrepareChangeSize(originalValue, pivotPoint, newSize, out begin, out end);
            return this.GetValue(begin, end);
        }
        /// <summary>
        /// Returns a Value from original Value, fixed point on axis (pivot tick) and Zoom ratio
        /// </summary>
        /// <param name="originalValue">Old value of range</param>
        /// <param name="pivotPoint">Fixed point = Pivot point (same point on old value and on new value)</param>
        /// <param name="zoomRatio"></param>
        /// <returns></returns>
        public virtual TValue CalculateValueByRatio(TValue originalValue, TTick pivotPoint, double zoomRatio)
        {
            TTick begin, end;
            this._ValueHelper.PrepareChangeSize(originalValue, pivotPoint, zoomRatio, out begin, out end);
            return this.GetValue(begin, end);
        }
        /// <summary>
        /// Returns a new Value from original Value, and specified relative Shift.
        /// Size of Value will be unchanged.
        /// Returned value is not aligned (does not call AlignValue()).
        /// Relative Shift is ratio, which determine fragment of Size, representing shift for Begin and End.
        /// Begin will be shifted by (Size * shiftRatio), for example: for shiftRatio == 1.0 will be new Begin = old End.
        /// When shiftRatio = 0.25 and value = { 10, 30 }, then returned value will be shifted by 0.25 * Size (=0.25 * 20 = 5) = { 15, 35 }.
        /// Value of shiftRatio can be negative, can be greater than 1. When shiftRatio == 0, then returned Value == clone of original Value.
        /// </summary>
        /// <param name="originalValue">Old value of range</param>
        /// <param name="shiftRatio">Ratio of value.Size for shift Value</param>
        /// <returns></returns>
        public virtual TValue CalculateValueByShift(TValue originalValue, double shiftRatio)
        {
            return this.CalculateValueByShift(originalValue, shiftRatio, AxisTickType.Pixel);
        }
        /// <summary>
        /// Returns a new Value from original Value, and specified relative Shift.
        /// Size of Value will be unchanged.
        /// Returned value is not aligned (does not call AlignValue()).
        /// Relative Shift is ratio, which determine fragment of Size, representing shift for Begin and End.
        /// Begin will be shifted by (Size * shiftRatio), for example: for shiftRatio == 1.0 will be new Begin = old End.
        /// When shiftRatio = 0.25 and value = { 10, 30 }, then returned value will be shifted by 0.25 * Size (=0.25 * 20 = 5) = { 15, 35 }.
        /// Value of shiftRatio can be negative, can be greater than 1. When shiftRatio == 0, then returned Value == clone of original Value.
        /// </summary>
        /// <param name="originalValue">Old value of range</param>
        /// <param name="shiftRatio">Ratio of value.Size for shift Value</param>
        /// <param name="roundType">Round type for rounding shifted value of Begin, null = no round</param>
        /// <returns></returns>
        public virtual TValue CalculateValueByShift(TValue originalValue, double shiftRatio, AxisTickType? roundType)
        {
            if (shiftRatio == 0d) return this.GetValue(originalValue);
            TSize shiftSize = this._ValueHelper.Multiply(originalValue.Size, (decimal)shiftRatio);
            return this.CalculateValueByShift(originalValue, shiftSize, roundType);
        }
        /// <summary>
        /// Returns a new Value from original Value, and specified absolute Shift Size.
        /// Size of Value will be unchanged.
        /// Returned value is not aligned (does not call AlignValue()).
        /// </summary>
        /// <param name="originalValue">Old value of range</param>
        /// <param name="shiftSize">Amount of Size for shift entire Value</param>
        /// <param name="roundType">Round type for rounding shifted value of Begin, null = no round</param>
        /// <returns></returns>
        public virtual TValue CalculateValueByShift(TValue originalValue, TSize shiftSize, AxisTickType? roundType)
        {
            TTick begin = this._ValueHelper.Add(originalValue.Begin, shiftSize);
            if (roundType.HasValue)
                begin = this.ArrangementCurrent.RoundValueToTick(begin, roundType.Value);
            return this.GetValueSize(begin, originalValue.Size);
        }
        /// <summary>
        /// Returns a clone of value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual TValue GetValue(TValue value)
        {
            return (value == null ? default(TValue) : this.GetValue(value.Begin, value.End));
        }
        /// <summary>
        /// Returns a Value from Begin and Size.
        /// </summary>
        /// <param name="begin">Value for Begin</param>
        /// <param name="size">Size of interval. End = Begin + Size.</param>
        /// <returns></returns>
        protected virtual TValue GetValueSize(TTick begin, TSize size)
        {
            TTick end = this._ValueHelper.Add(begin, size);
            return this.GetValue(begin, end);
        }
        /// <summary>
        /// Returns a number of logical units (Decimal) for specified number of pixels, using current Scale value.
        /// Scale is number of logical units (Decimal) to one pixel, thus GetUnitsFromPixels() returns (Scale * pixels).
        /// Can return zero.
        /// </summary>
        /// <param name="pixels"></param>
        /// <returns></returns>
        protected decimal GetUnitsFromPixels(decimal pixels)
        {
            return this._Scale * pixels;
        }
        /// <summary>
        /// Returns a number of pixels for specified number of logical units (Decimal?), using current Scale value.
        /// Scale is number of logical units (Decimal) to one pixel, thus GetUnitsFromPixels() returns (Scale * pixels).
        /// Can return null.
        /// </summary>
        /// <param name="units"></param>
        /// <returns></returns>
        protected decimal? GetPixelsFromUnits(decimal? units)
        {
            return ((units.HasValue && this._Scale > 0m) ? (decimal?)(units.Value / this._Scale) : (decimal?)null);
        }
        /// <summary>
        /// Returns a local pixel distance from Axis.FirstPixel, for specified TTick (this is a logical value).
        /// Can returns a null, when this.GetAxisUnits(tick - this.Value) returns a null value.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        private decimal? _CalculatePixelLocalForTick(TTick tick)
        {
            TSize size = this._ValueHelper.SubSize(tick, this.Value.Begin);    // Distance from tick to Begin (positive when tick is greater than Begin)
            decimal? units = this.GetAxisUnits(size);                          // Decimal number of logical units (or null)
            decimal? pixels = this.GetPixelsFromUnits(units);                  // Distance in pixels by units and Scale (or null)
            return pixels;
        }
        #endregion
        #region Set and Detect methods - core of Axis for inner reacalculations and change values
        /*    Akce a reakce objektu BaseAxis na změny / vložení hodnot:
            Změna rozměru (interaktivní i programová) : 
              > VisibleRelativeBounds (zvenku, public) => 
                    - nastavení odpovídající orientace:                                       DetectOrientation()
                    - přepočet hodnoty End (změnil se rozměr osy, ale ne měřítko):            DetectValueEnd()
                        - ten v sobě zahrnuje přepočet Ticků
                    - anebo pokud není požadován přepočet End, pak přepočet Scale             DetectScale()
                        - ten v sobě zahrnuje přepočet 
                    - zajištění vykreslení osy
              > VisibleRelativeBoundsSilent (zevnitř) =>
                    - nastavení odpovídající orientace:                                       DetectOrientation()

        */

        /// <summary>
        /// Is called after Bounds change, from SetBound() method, without any conditions (even if action is None).
        /// Recalculate ValueEnd, Align Value to Range, recalculate Scale, when Size of value is changed.
        /// When new value is equal to current value, ends (no events is called).
        /// When there is a change of value, then events specified in (actions) parameter is raised.
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected override void SetBoundsAfterChange(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            if (this._LastSize != null && this._LastSize.Value == newBounds.Size) return;

            this.DetectOrientation(LeaveOnlyActions(actions, ProcessAction.CallChangedEvents), eventSource);

            // Po změně velikosti můžeme buď změnit Value, nebo Scale:
            AxisResizeContentMode mode = this.ResizeContentMode;
            ProcessAction subActions = actions;
            switch (mode)
            {
                case AxisResizeContentMode.None:
                case AxisResizeContentMode.ChangeValueEnd:
                    subActions = LeaveOnlyActions(subActions, ProcessAction.PrepareInnerItems, ProcessAction.CallChangedEvents, ProcessAction.CallSynchronizeSlave);
                    this.DetectValueEnd(subActions, eventSource);
                    break;
                case AxisResizeContentMode.ChangeScale:
                    subActions = LeaveOnlyActions(subActions, ProcessAction.RecalcInnerData, ProcessAction.PrepareInnerItems, ProcessAction.CallChangedEvents, ProcessAction.CallSynchronizeSlave);
                    subActions = AddActions(subActions, ProcessAction.RecalcInnerData, ProcessAction.PrepareInnerItems);
                    this.DetectScale(subActions, eventSource);
                    break;
            }

            this._LastSize = newBounds.Size;
        }
        /// <summary>
        /// Velikost, pro kterou byly naposledy přepočteny vnitřní data. Null = dosud nepřepočteny.
        /// </summary>
        private Size? _LastSize;
        /// <summary>
        /// Accomodate this.OrientationCurrent to this.OrientationUser and this.VisibleRelativeBounds
        /// </summary>
        /// <param name="actions">Akce</param>
        /// <param name="eventSource">Zdroj události</param>
        internal void DetectOrientation(ProcessAction actions, EventSourceType eventSource)
        {
            decimal oldPixel = this.PixelSize;
            AxisOrientation oldOrientation = this.OrientationCurrent;
            AxisOrientation newOrientation = this.GetValidOrientation();
            if (newOrientation == oldOrientation) return;

            this.OrientationCurrent = newOrientation;
            decimal newPixel = this.PixelSize;

            if (newPixel != oldPixel && (IsAction(actions, ProcessAction.RecalcValue)))
                this.DetectValueEnd(actions, eventSource);

            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallOrientationChanged(oldOrientation, newOrientation, eventSource);

            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Calculate Value.End from Value.Begin and current Scale and AxisSizeInPixel.
        /// Store new value to this.Value, 
        /// </summary>
        internal void DetectValueEnd(ProcessAction actions, EventSourceType eventSource)
        {
            if (!this.IsVisualValid) return;
            if (!this.Value.HasBegin) return;

            if (!this.IsScaleValid)
                this._Scale = this.GetCurrentScale();
            if (!this.IsScaleValid) return;

            // From PixelSize and Scale we calculate Size, from Value.Begin and Size we calculate End value:
            decimal pixel = this.PixelSize;
            decimal units = pixel * this.Scale;
            TSize size = this.GetAxisSize(units);

            TValue oldValue = this.Value;
            TTick end = this._ValueHelper.Add(oldValue.Begin, size);
            TValue newValue = this.GetValue(oldValue.Begin, end);

            if (this.IsEqual(newValue, oldValue)) return;

            // Changing the Value causes all (except recalculation Scale and Arrangements). Call DetectTicks().
            this.SetValue(newValue, RemoveActions(actions, ProcessAction.RecalcScale, ProcessAction.RecalcInnerData), eventSource);
        }
        /// <summary>
        /// Zjistí, jakou hodnotu by mělo mít měřítko (Scale) pro současnou osu (Pixely : AxisSizeInPixel, Value.Size).
        /// Pokud dojde ke změně Scale, uloží si novou hodnotu a vyvolá eventy:
        /// DetectArrangement() { DetectTicks() { CallTicksChanged(); CallDrawRequest(); } CallArrangementChanged(); } CallScaleChanged();
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        internal void DetectScale(ProcessAction actions, EventSourceType eventSource)
        {
            if (!this.IsVisualValid) return;
            if (!this.Value.HasBegin) return;

            // From Axis.Size and Value.Size we calculate Scale:
            decimal oldScale = this._Scale;
            decimal newScale = this.GetCurrentScale();
            if (newScale <= 0m || oldScale == newScale) return;

            this._Scale = newScale;
            this._LastTickPositionDict = null;

            if (IsAction(actions, ProcessAction.RecalcInnerData))
                this.DetectArrangement(actions, eventSource);

            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallScaleChanged(oldScale, newScale, eventSource);
        }
        /// <summary>
        /// Calculate current scale (from: this.Value.Size, this.GetAxisUnits(), this.PixelSize) and returns it.
        /// When GetAxisUnits() returns null or non-positive number, returns 0.
        /// When PixelSize is smaller than this.PixelSizeMinimum (default = 20 pixels), returns 0.
        /// </summary>
        /// <returns></returns>
        protected decimal GetCurrentScale()
        {
            decimal? units = this.GetAxisUnits(this.Value.Size);     // Number of logical units on axis
            if (!units.HasValue || units.Value <= 0m) return 0m;     // Not defined, or not positive: not accepted
            decimal pixels = this.PixelSize;
            if (pixels < this.PixelSizeMinimum) return 0m;           // NonVisual axis
            decimal scale = AlignScale(units.Value / pixels);
            return scale;
        }
        /// <summary>
        /// Pro aktuální měřítko (Scale = počet datových jednotek na jeden pixel)  najde nejvhodnější ArrangementOne,
        /// a pokud se bude lišit od aktuálního, pak nově vytvoří Ticky pro osu, a zavolá event CallArrangementChanged().
        /// Může vyvolat metody: DetectTicks(); { CallTicksChanged(); CallDrawRequest(); } CallArrangementChanged();
        /// </summary>
        internal void DetectArrangement(ProcessAction actions, EventSourceType eventSource)
        {
            if (!this.IsAxisValid) return;

            ArrangementOne oldArrangement = this.ArrangementCurrent;
            ArrangementOne newArrangement = this.GetCurrentArrangement();
            this.ArrangementCurrent = newArrangement;

            if (this.IsCurrentTicksValid) return;

            if (IsAction(actions, ProcessAction.PrepareInnerItems))
                this.DetectTicks(actions, eventSource);

            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallArrangementChanged(oldArrangement, newArrangement, eventSource);
        }
        /// <summary>
        /// Vrátí ArrangementOne pro aktuální měřítko (Scale).
        /// </summary>
        /// <returns></returns>
        protected ArrangementOne GetCurrentArrangement()
        {
            return this.Arrangements.SelectSetForScale(this.Scale); ;
        }
        /// <summary>
        /// Zajistí, že pole TickList bude obsahovat platné Ticky pro aktuální osu.
        /// Vyvolá požadované eventy (CallTicksChanged(); CallDrawRequest()), pokud dojde ke změně v naplnění pole TickList.
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        internal void DetectTicks(ProcessAction actions, EventSourceType eventSource)
        {
            if (this.ArrangementCurrent == null) return;
            if (this.IsCurrentTicksValid) return;

            BaseTick<TTick>[] oldTickList = this.TickList;
            this.TickList = this.GetCurrentTicks();
            BaseTick<TTick>[] newTickList = this.TickList;

            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallTicksChanged(oldTickList, newTickList, eventSource);

            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Vytvoří a vrátí pole Ticků pro aktuální osu (arrangement, měřítko, hodnota, počet pixelů).
        /// </summary>
        /// <returns></returns>
        protected virtual BaseTick<TTick>[] GetCurrentTicks()
        {
            ArrangementOne arrangementCurrent = this.ArrangementCurrent;

            // Založíme Dictionary, kde klíčem je typ Tick (pozice na ose), a hodnotou je vytvořený Tick:
            Dictionary<TTick, BaseTick<TTick>> tickDict = new Dictionary<TTick, BaseTick<TTick>>();

            // Můžeme využít data o dosavadních pozicích Ticků:
            Dictionary<TTick, BaseTick<TTick>> lastTickDict = this._LastTickPositionDict;
            int? currentPixelOffset = null;                          // O kolik se posunuly Ticky od posledního výpočtu

            // Do Dictionary naplníme jednotlivé Ticky, počínaje těmi s nejvyšší důležitostí:
            arrangementCurrent.AddInitialTicks(tickDict);
            arrangementCurrent.CalculateTicksLine(AxisTickType.BigLabel, tickDict, lastTickDict, ref currentPixelOffset);
            arrangementCurrent.CalculateTicksLine(AxisTickType.StdLabel, tickDict, lastTickDict, ref currentPixelOffset);
            arrangementCurrent.CalculateTicksLine(AxisTickType.BigTick, tickDict, lastTickDict, ref currentPixelOffset);
            arrangementCurrent.CalculateTicksLine(AxisTickType.StdTick, tickDict, lastTickDict, ref currentPixelOffset);

            // Připravené Ticky převezmu z Dictionary do Listu a setřídím je podle jejich hodnoty:
            List<BaseTick<TTick>> newTickList = new List<BaseTick<TTick>>(tickDict.Values);
            newTickList.Sort((a, b) => this._ValueHelper.CompareByEdge(a.Value, b.Value));

            // Potlačíme zobrazení labelů na Ticku typu Initialial, pokud jeho text je identický 
            //  jako text na prvním / posledním Ticku typu BigLabel (byly by dva shodné texty poblíž u sebe):
            this.ModifyTicksLabels(newTickList, arrangementCurrent);
           
            // Uložíme aktuálně použité hodnoty jako Last, abychom dokázali příště poznat změnu dat, která má vést k přepočtům na ose:
            this._LastTickArrangement = arrangementCurrent;
            this._LastTickSize = this.PixelSize;
            this._LastTickScale = this.Scale;
            this._LastTickValue = this.Value;

            this._LastTickPositionDict = tickDict;

            return newTickList.ToArray();
        }
        /// <summary>
        /// Metoda upraví popisky jednotlivých Ticků v dodaném poli tak, aby byly "rozumné".
        /// Tzn. aby se neopakovaly popisky krajních ticků (=typ Initial) s popisky nejbližších BigLabel ticků.
        /// </summary>
        /// <param name="tickList"></param>
        /// <param name="arrangementCurrent"></param>
        protected virtual void ModifyTicksLabels(List<BaseTick<TTick>> tickList, ArrangementOne arrangementCurrent)
        {
            int tickCount = tickList.Count;
            if (tickCount > 0)
            {
                BaseTick<TTick> firstTick = tickList[0];
                if (firstTick.TickType == AxisTickType.OuterLabel)
                {
                    BaseTick<TTick> titleTick = tickList.FirstOrDefault(t => t.TickType == AxisTickType.BigLabel);
                    if (titleTick != null && titleTick.Text == firstTick.Text)
                        firstTick.Text = "";
                }

                BaseTick<TTick> lastTick = tickList[tickCount - 1];
                if (lastTick.TickType == AxisTickType.OuterLabel)
                {
                    BaseTick<TTick> titleTick = tickList.LastOrDefault(t => t.TickType == AxisTickType.BigLabel);
                    if (titleTick != null && titleTick.Text == lastTick.Text)
                        lastTick.Text = "";
                }
            }
        }
        /// <summary>
        /// Vrátí hodnotu Ticku zarovnanou pro první pozici daného aranžmá na časové ose.
        /// Bázová metoda vrátí dodaný Tick zaokrouhlený nahoru na ucelený interval daného aranžmá.
        /// Potomek může vrátit hodnotu určenou jinak, například TimeAxis může pracovat s týdny i kvartály...
        /// </summary>
        /// <param name="tick">Pozice na ose, bez zaokrouhlení</param>
        /// <param name="item">Položka aranžmá osy a konkrétního Ticku</param>
        /// <returns></returns>
        protected virtual TTick RoundFirstTickForArrangement(TTick tick, ArrangementItem item)
        {
            return this.RoundTickToInterval(tick, item.Interval, RoundMode.Ceiling);
        }
        /// <summary>
        /// Vrátí hodnotu Ticku zarovnanou pro další (=ne první) pozici daného aranžmá na časové ose.
        /// Bázová metoda vrátí dodaný Tick zaokrouhlený matematicky na ucelený interval daného aranžmá.
        /// Potomek může vrátit hodnotu určenou jinak, například TimeAxis může pracovat s týdny i kvartály...
        /// </summary>
        /// <param name="tick">Pozice na ose, bez zaokrouhlení</param>
        /// <param name="item">Položka aranžmá osy a konkrétního Ticku</param>
        /// <returns></returns>
        protected virtual TTick RoundNextTickForArrangement(TTick tick, ArrangementItem item)
        {
            return this.RoundTickToInterval(tick, item.Interval, RoundMode.Math);
        }
        /// <summary>
        /// Arrangement, pro který byl naposledy generován soupis Ticků
        /// </summary>
        protected ArrangementOne _LastTickArrangement;
        /// <summary>
        /// Velikost osy v pixelech, pro kterou byl naposledy generován soupis Ticků
        /// </summary>
        protected Decimal? _LastTickSize;
        /// <summary>
        /// Měřítko osy, pro které byl naposledy generován soupis Ticků
        /// </summary>
        protected Decimal? _LastTickScale;
        /// <summary>
        /// Hodnota osy (Begin, End), pro kterou byl naposledy generován soupis Ticků
        /// </summary>
        protected TValue _LastTickValue;
        /// <summary>
        /// Index ticků, jak byly posledně vygenerovány.
        /// Slouží při tvorbě nových Ticků jako podklad o fyzické pozici předchozích ticků (souřadnice v pixelech),
        /// důvodem je "hladké" posouvání celé sady ticků po ose při běžném shiftu pomocí posouván myší.
        /// Pokud by neexistovala data o předchozích pozicích ticků, pak se pozice (v pixelech) pro nové ticky počítá matematicky podle měřítka,
        /// což je jistě správný postup. Ale musí se při něm provést zaokrouhlení pozice ticku na celé pixely (nepodporujeme kreslení ticků na pixely
        /// s float souřadnicí). I toto zaokrouhlení je tedy opodstatněné. Ale po přesunu dat na ose o 1 pixel (posun myší) se vypočte nová hodnota Value.Begin,
        /// následně se vypočtou nové pozice jednotlivých Ticků na ose, ty se zaokrouhlí na celé pixely, ale to může být JINÉ ZAOKROUHLENÍ než pro minulé Value.Begin.
        /// Důsledkem toho všeho je stav, kdy při posouvání osy některé ticky na ose poskakují v jiném rytmu, než ostatní.
        /// S použitím _LastTickPositionDict se využijí pixelové pozice ticků z předchozího stavu pro nově vypočítávané ticky, důsledkem je tedy hladký posun.
        /// Pro ilustraci postačí nastavit this._IgnoreLastTickPositionDict na true a posouvat osu.
        /// Je nastaveno na null po každé změně Scale, protože pak tento princip neplatí = pak je nutno reálně znovu napočítat pozice Ticků podle nového měřítka.
        /// </summary>
        protected Dictionary<TTick, BaseTick<TTick>> _LastTickPositionDict;
        /// <summary>
        /// jen pro otestování chování this._LastTickPositionDict (viz komentář k _LastTickPositionDict).
        /// </summary>
        protected bool _IgnoreLastTickPositionDict = false;
        /// <summary>
        /// Obsahuje true, pokud dříve vygenerované ticky (pomocí metody GetCurrentTicks()) jsou stále platné 
        /// pro aktuální stav (ArrangementCurrent, PixelSize, Scale, Value).
        /// </summary>
        protected bool IsCurrentTicksValid
        {
            get
            {
                if (this._LastTickArrangement == null || !this._LastTickSize.HasValue || !this._LastTickScale.HasValue || this._LastTickValue == null)
                    return false;

                if (!ArrangementOne.IsEqual(_LastTickArrangement, this.ArrangementCurrent)) return false;
                if (this._LastTickSize != this.PixelSize) return false;
                if (this._LastTickScale != this.Scale) return false;
                if (this._LastTickValue != this.Value) return false;

                return true;
            }
        }
        /// <summary>
        /// Set a new Value to this Axis.
        /// Apply limits by ValueRange.
        /// Recalculate Scale, when Size of value is changed.
        /// When is new value equal to current value, ends (no events is called).
        /// When is there a change of value, then events specified in (actions) parameter is raised.
        /// </summary>
        /// <param name="value">New value, un-aligned, can be empty or null</param>
        /// <param name="actions">Actions to be taken</param>
        /// <param name="eventSource">Source of this event, will be send to event handlers</param>
        internal void SetValue(TValue value, ProcessAction actions, EventSourceType eventSource)
        {
            TValue oldValue = this._Value;
            TValue newValue = this.AlignValue(value);
            if (this.IsEqual(oldValue, newValue)) return;            // No change = no reactions.

            this._Value = newValue;
            this._ISegmentsCurrent = null;

            if (IsAction(actions, ProcessAction.RecalcScale))
            {
                decimal oldScale = this._Scale;
                decimal newScale = this.CalculateScale(newValue);    // Calculate new Scale
                if (oldScale != newScale)
                {
                    this._Scale = newScale;
                    if (IsAction(actions, ProcessAction.RecalcInnerData))
                        this.DetectArrangement(RemoveActions(actions, ProcessAction.CallDraw), eventSource);

                    if (IsAction(actions, ProcessAction.CallChangedEvents))
                        this.CallScaleChanged(oldScale, newScale, eventSource);
                }
            }

            if (IsAction(actions, ProcessAction.PrepareInnerItems))
                this.DetectTicks(actions, eventSource);

            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallValueChanged(oldValue, newValue, eventSource);

            if (IsAction(actions, ProcessAction.CallSynchronizeSlave))
                this.CallSlaveSynchronize(oldValue, newValue, eventSource);

            this._ISegmentsCurrent = null;

            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Set a new ValueRange to this Axis.
        /// Apply these new limits to Value.
        /// Recalculate Scale, when Size of value is changed.
        /// When is new value equal to current value, ends (no events is called).
        /// When is there a change of value, then events specified in (actions) parameter is raised.
        /// </summary>
        /// <param name="valueRange"></param>
        /// <param name="actions">Actions to be taken</param>
        /// <param name="eventSource">Source of this event, will be send to event handlers</param>
        internal void SetValueLimit(TValue valueRange, ProcessAction actions, EventSourceType eventSource)
        {
            TValue oldValueRange = this._ValueLimit;
            TValue newValueRange = valueRange;
            if (this.IsEqual(oldValueRange, newValueRange)) return;  // No change = no reactions.

            this._ValueLimit = newValueRange;
            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallValueLimitChanged(oldValueRange, newValueRange, eventSource);

            this.SetValue(this._Value, actions, eventSource);
        }
        /// <summary>
        /// Set a new ValueRange to this Axis.
        /// Apply these new limits to Value.
        /// Recalculate Scale, when Size of value is changed.
        /// When is new value equal to current value, ends (no events is called).
        /// When is there a change of value, then events specified in (actions) parameter is raised.
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="actions">Actions to be taken</param>
        /// <param name="eventSource">Source of this event, will be send to event handlers</param>
        internal void SetScale(decimal scale, ProcessAction actions, EventSourceType eventSource)
        {
            this.SetScale(scale, null, actions, eventSource);
        }
        /// <summary>
        /// Set a new ValueRange to this Axis.
        /// Apply these new limits to Value.
        /// Recalculate Scale, when Size of value is changed.
        /// When is new value equal to current value, ends (no events is called).
        /// When is there a change of value, then events specified in (actions) parameter is raised.
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="pivotPoint">Pevný bod, relativní pozice</param>
        /// <param name="actions">Actions to be taken</param>
        /// <param name="eventSource">Source of this event, will be send to event handlers</param>
        internal void SetScale(decimal scale, decimal? pivotPoint, ProcessAction actions, EventSourceType eventSource)
        {
            if (scale <= 0m) return;

            decimal oldScale = this._Scale;
            decimal newScale = this.AlignScale(scale);
            if (oldScale == newScale) return;                        // No change = no reactions.
            this._ISegmentsCurrent = null;

            if (IsAction(actions, ProcessAction.RecalcValue))
            {   // All changes will be processed as change of Value (from this will be changed Scale and called all events):
                TValue oldValue = this._Value;
                TValue newValue = this.ChangeValueToScaleAlign(oldValue, newScale, pivotPoint);
                ProcessAction valueActions = (ProcessAction)(actions | ProcessAction.RecalcScale);
                this.SetValue(newValue, valueActions, eventSource);  // actions with activated flag RecalcScale (after Align(newValue) to ValueRange can Scale be different from current newScale)
            }
            else
            {   // "Silent" change only to Scale:
                this._Scale = newScale;

                if (IsAction(actions, ProcessAction.CallChangedEvents))
                    this.CallScaleChanged(oldScale, newScale, eventSource);
            }
        }
        /// <summary>
        /// Set a new ValueRange to this Axis.
        /// Apply these new limits to Value.
        /// Recalculate Scale, when Size of value is changed.
        /// When is new value equal to current value, ends (no events is called).
        /// When is there a change of value, then events specified in (actions) parameter is raised.
        /// </summary>
        /// <param name="scaleRange"></param>
        /// <param name="actions">Actions to be taken</param>
        /// <param name="eventSource">Source of this event, will be send to event handlers</param>
        internal void SetScaleLimit(DecimalNRange scaleRange, ProcessAction actions, EventSourceType eventSource)
        {
            DecimalNRange oldScaleRange = this._ScaleLimit;
            DecimalNRange newScaleRange = scaleRange;
            if (this.IsEqual(oldScaleRange, newScaleRange)) return;  // No change = no reactions.

            this._ScaleLimit = newScaleRange;

            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallScaleLimitChanged(oldScaleRange, newScaleRange, eventSource);

            if (IsAction(actions, ProcessAction.RecalcValue))
            {
                TValue oldValue = this._Value;
                ProcessAction valueActions = (ProcessAction)(actions | ProcessAction.RecalcScale | ProcessAction.RecalcValue);
                this.SetValue(oldValue, valueActions, eventSource);   // actions with activated flag RecalcScale and RecalcValue (after Align(newValue) to ValueRange can Scale be different from current newScale)
            }
        }
        /// <summary>
        /// Combine specified value with current Value. Align new value to ValueRange, calculate new Scale, and align Scale to ScaleRange (recalculate new Value).
        /// Does not write any values to this instance.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal TValue AlignValue(TValue value)
        {
            TValue oldValue = this._Value;

            // New value from parameter, or from this.Value (mode for apply new ValueRange and/or ScaleRange):
            TValue newValue;
            if (value == null || value.IsEmpty)
                newValue = oldValue;                                 // New value is not specified, we use current Value
            else
                newValue = this.ApplyNewValue(value);                // Returns Begin and End from value, but if Begin or End is empty, then returns Begin or End from this.Value

            newValue = this.AlignValueToRange(newValue);             // Align newValue to this.ValueRange, preserve newValue.Size if is it possible
            decimal? newScale = this.CalculateScaleN(newValue);      // Calculate new Scale (with null value enabled)

            // Apply ScaleRange:
            if (newScale.HasValue && this._ScaleLimit != null && !this._ScaleLimit.IsEmpty)
            {   // Can apply ScaleRange?
                decimal? alignedScale = this._ScaleLimit.Align(newScale);
                if (alignedScale.HasValue && alignedScale.Value > 0m && alignedScale.Value != newScale.Value)
                {   // New value has scale, which is posiive, but not allowed by ScaleRange:
                    // We must change Value to accept aligned ScaleRange:
                    // This is: from alignedScale and current pixel size we calculate new TSize for Value, and then Zoom value do target size:
                    newValue = this.ChangeValueToScaleAlign(newValue, alignedScale.Value);
                }
            }

            return newValue;
        }
        /// <summary>
        /// Return specified scale aligned into this.ScaleRange.
        /// When result scale is invalid (is not positive number), returns current Scale.
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        internal decimal AlignScale(decimal scale)
        {
            decimal newScale = scale;
            if (this._ScaleLimit != null && !this._ScaleLimit.IsEmpty)
            {
                decimal? alignedScale = this._ScaleLimit.Align(newScale);
                if (alignedScale.HasValue)
                    newScale = alignedScale.Value;
            }
            return (newScale > 0m ? newScale : this._Scale);
        }
        /// <summary>
        /// Returns a valid orientation for current OrientationUser and VisibleRelativeBounds.
        /// </summary>
        /// <returns></returns>
        internal AxisOrientation GetValidOrientation()
        {
            AxisOrientation orientation = this.OrientationCurrent;
            Rectangle bounds = this.Bounds;
            bool isHorizontal = (2 * bounds.Width > 3 * bounds.Height);        // Rectangle with ratio Width:Height = 3:2 is accepted as "standard", 
            bool hasUserOrientation = this.OrientationUser.HasValue;
            if (isHorizontal)
            {   // Current bounds are horizontal:
                if (hasUserOrientation && (this.OrientationUser.Value == AxisOrientation.Top || this.OrientationUser.Value == AxisOrientation.Bottom))
                {   // Have a user request to orientation, and is horizontal => use it:
                    orientation = this.OrientationUser.Value;
                }
                else
                {   // does not have a correct user orientation, set current orientation by position of axis:
                    orientation = ((bounds.Y < 100) ? AxisOrientation.Top : AxisOrientation.Bottom);
                }
            }
            else
            {   // Current bounds are vertical:
                if (hasUserOrientation && (this.OrientationUser.Value == AxisOrientation.LeftUp || this.OrientationUser.Value == AxisOrientation.LeftDown || this.OrientationUser.Value == AxisOrientation.RightUp || this.OrientationUser.Value == AxisOrientation.RightDown))
                {   // Have a user request to orientation, and is horizontal => use it:
                    orientation = this.OrientationUser.Value;
                }
                else
                {   // does not have a correct user orientation, set current orientation by position of axis:
                    orientation = ((bounds.X < 100) ? AxisOrientation.LeftDown : AxisOrientation.RightDown);
                }
            }
            return orientation;
        }
        /// <summary>
        /// Calculate required field : Scale (if is not a positive number) and Ticks (if is null or empty).
        /// </summary>
        protected void CalculateRequiredEntities()
        {
            if (this.PixelSize < this.PixelSizeMinimum) return;
            if (this.Arrangements == null || this.Arrangements.IsEmpty) return;

            if (this._Scale <= 0m)
                this._Scale = this.GetCurrentScale();

            if (this.ArrangementCurrent == null)
            {
                this.ArrangementCurrent = this.GetCurrentArrangement();
                this.TickList = null;
            }

            if (this.TickList == null)
                this.TickList = this.GetCurrentTicks();
        }
        #region Support for align value and scale
        /// <summary>
        /// Returns a value, where instead of a blank edge (Begin, End) will be edge from this.Value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private TValue ApplyNewValue(TValue value)
        {
            TTick begin, end;
            this._ValueHelper.PrepareApplyNewValue(value, this._Value, true, out begin, out end);
            return this.GetValue(begin, end);
        }
        /// <summary>
        /// Returns a value aligned to this.ValueRange, with preserving value.Size
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private TValue AlignValueToRange(TValue value)
        {
            if (this._ValueLimit == null || this._ValueLimit.IsEmpty) return value;

            TTick begin, end;
            this._ValueHelper.PrepareAlign(value, this._ValueLimit, true, out begin, out end);
            return this.GetValue(begin, end);
        }
        /// <summary>
        /// Returns a new value, calculated from specified value and new scale.
        /// Returns value is not aligned to this.ValueRange.
        /// Pivot point (fixed point in range) is on 1/2 of range.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private TValue ChangeValueToScale(TValue value, decimal scale)
        {
            return this._ChangeValueToScaleAlign(value, scale, 0.5m, false);
        }
        /// <summary>
        /// Returns a new value, calculated from specified value and new scale.
        /// Returns value is aligned to this.ValueRange.
        /// Pivot point (fixed point in range) is on 1/2 of range.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private TValue ChangeValueToScaleAlign(TValue value, decimal scale)
        {
            return this._ChangeValueToScaleAlign(value, scale, 0.5m, true);
        }
        /// <summary>
        /// Returns a new value, calculated from specified value and new scale.
        /// Returns value is not aligned to this.ValueRange.
        /// Pivot point (fixed point in range) is on specified point.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="scale"></param>
        /// <param name="pivotPoint"></param>
        /// <returns></returns>
        private TValue ChangeValueToScale(TValue value, decimal scale, TTick pivotPoint)
        {
            decimal? pivotRatio = value.GetRelativePositionAtValue(pivotPoint);
            return this._ChangeValueToScaleAlign(value, scale, pivotRatio, false);
        }
        /// <summary>
        /// Returns a new value, calculated from specified value and new scale.
        /// Returns value is aligned to this.ValueRange.
        /// Pivot point (fixed point in range) is on specified point.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="scale"></param>
        /// <param name="pivotPoint"></param>
        /// <returns></returns>
        private TValue ChangeValueToScaleAlign(TValue value, decimal scale, TTick pivotPoint)
        {
            decimal? pivotRatio = value.GetRelativePositionAtValue(pivotPoint);
            return this._ChangeValueToScaleAlign(value, scale, pivotRatio, true);
        }
        /// <summary>
        /// Returns a new value, calculated from specified value and new scale.
        /// Returns value is not aligned to this.ValueRange.
        /// Pivot point (fixed point in range) is on specified relative position of range.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="scale"></param>
        /// <param name="pivotRatio"></param>
        /// <returns></returns>
        private TValue ChangeValueToScale(TValue value, decimal scale, decimal? pivotRatio)
        {
            return this._ChangeValueToScaleAlign(value, scale, pivotRatio, false);
        }
        /// <summary>
        /// Returns a new value, calculated from specified value and new scale.
        /// Returns value is aligned to this.ValueRange.
        /// Pivot point (fixed point in range) is on specified relative position of range.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="scale"></param>
        /// <param name="pivotRatio"></param>
        /// <returns></returns>
        private TValue ChangeValueToScaleAlign(TValue value, decimal scale, decimal? pivotRatio)
        {
            return this._ChangeValueToScaleAlign(value, scale, pivotRatio, true);
        }
        /// <summary>
        /// Returns a new value, calculated from specified value and new scale.
        /// Returns value is aligned to this.ValueRange by parameter align.
        /// Pivot point (fixed point in range) is on specified relative position of range.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="scale"></param>
        /// <param name="pivotRatio"></param>
        /// <param name="align"></param>
        /// <returns></returns>
        private TValue _ChangeValueToScaleAlign(TValue value, decimal scale, decimal? pivotRatio, bool align)
        {
            TSize size = this.CalculateSize(scale);                  // Size for new Value, from scale and current pixel size of Axis
            TTick begin, end;
            this._ValueHelper.PrepareChangeSize(value, size, pivotRatio, out begin, out end);
            TValue newValue = this.GetValue(begin, end);
            if (align)
                newValue = this.AlignValueToRange(newValue);
            return this.GetValue(begin, end);
        }
        /// <summary>
        /// Return scale for specified logical value, on current physical axis bounds (physical size of axis = this.PixelSize).
        /// Return this.Scale when values are not correct (value is empty or size is non-positive, or current pixel size of axis is not positive).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private decimal CalculateScale(TValue value)
        {
            return (value != null ? this.CalculateScale(value.Size) : this._Scale);
        }
        /// <summary>
        /// Return scale for specified logical value, on current physical axis bounds (physical size of axis = this.PixelSize).
        /// Return this.Scale when values are not correct (value is empty or size is non-positive, or current pixel size of axis is not positive).
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private decimal CalculateScale(TSize size)
        {
            decimal? units = this.GetAxisUnits(size);                          // Number of logical units on axis
            if (!units.HasValue || units.Value < 0m) return this._Scale;       // Not defined, or not positive: not accepted
            decimal pixels = this.PixelSize;
            if (pixels <= 1m) return this._Scale;                              // NonVisual axis
            return units.Value / pixels;
        }
        /// <summary>
        /// Return scale for specified logical value, on current physical axis bounds (physical size of axis = this.AxisSizeInPixel).
        /// Return null when values are not correct (value is empty or size is non-positive, or current pixel size of axis is not positive).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private decimal? CalculateScaleN(TValue value)
        {
            return (value != null ? this.CalculateScaleN(value.Size) : (decimal?)null);
        }
        /// <summary>
        /// Return scale for specified logical value, on current physical axis bounds (physical size of axis = this.AxisSizeInPixel).
        /// Return null when values are not correct (value is empty or size is non-positive, or current pixel size of axis is not positive).
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private decimal? CalculateScaleN(TSize size)
        {
            decimal? units = this.GetAxisUnits(size);                          // Number of logical units on axis
            if (!units.HasValue || units.Value < 0m) return null;              // Not defined, or not positive: not accepted
            decimal pixels = this.PixelSize;
            if (pixels <= 1m) return null;                                     // NonVisual axis
            return units.Value / pixels;
        }
        /// <summary>
        /// Returns logical TSize for current axis (for AxisSizeInPixel) for specified scale.
        /// Returns empty TSize (default) for invalid inputs.
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        private TSize CalculateSize(decimal? scale)
        {
            if (!scale.HasValue || scale.Value <= 0m) return default(TSize);   // Invalid scale
            decimal pixels = this.PixelSize;
            if (pixels <= 1m) return default(TSize);                           // NonVisual axis
            decimal units = scale.Value * pixels;
            return this.GetAxisSize(units);
        }
        /// <summary>
        /// Returns true, when two instance of Value type has equal values (or is equals empty).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool IsEqual(TValue a, TValue b)
        {
            return this._ValueHelper.IsEqual(a, b);
        }
        /// <summary>
        /// Returns true, when two instance of ScaleRange type has equal values (or is equals empty).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool IsEqual(DecimalNRange a, DecimalNRange b)
        {
            return this._ScaleHelper.IsEqual(a, b);
        }
        #endregion
        #endregion
        #region Arrangement of Axis: Init, CurrentSet; AxisTicks
        /// <summary>
        /// Current list of all ticks
        /// </summary>
        public IEnumerable<BaseTick<TTick>> Ticks { get { return this.TickList; } }
        /// <summary>
        /// Ensure all initialization of Arrangements (prepare instance, call abstract InitAxisArrangement(), perform Finish)
        /// </summary>
        private void PrepareArrangement()
        {
            this.TickList = new BaseTick<TTick>[0];
            this.Arrangements = new ArrangementSet(this);
            this.InitAxisArrangement();
            this.Arrangements.Finalise();
        }
        /// <summary>
        /// Add one arrangement to this set
        /// </summary>
        /// <param name="one"></param>
        protected void AddArrangementOne(ArrangementOne one)
        {
            this.Arrangements.AddOne(one);
        }
        /// <summary>
        /// Current list of all ticks
        /// </summary>
        protected BaseTick<TTick>[] TickList { get; private set; }
        /// <summary>
        /// A complete set of all disponible arrangement sets
        /// </summary>
        protected ArrangementSet Arrangements { get; private set; }
        /// <summary>
        /// Current ArrangementOne, valid for current Scale (Value and Size)
        /// </summary>
        protected ArrangementOne ArrangementCurrent { get; private set; }
        #region classes for Arrangement: ArrangementSet, ArrangementOne, ArrangementTick
        /// <summary>
        /// Set of all disponible Axis arrangements
        /// </summary>
        protected class ArrangementSet
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="axis"></param>
            public ArrangementSet(BaseAxis<TTick, TSize, TValue> axis)
            {
                this.Axis = axis;
                this._List = null;
                this._IsFinalised = false;
                this.SubTitleDistance = 65m;
            }
            /// <summary>
            /// Add one arrangement to this set
            /// </summary>
            /// <param name="one"></param>
            public void AddOne(ArrangementOne one)
            {
                if (this._IsFinalised)
                    throw new GraphLibCodeException("Nelze přidat další ArrangementOne do ArrangementSet poté, kdy proběhla finalizace.");

                if (one != null)
                {
                    if (this._List == null)
                        this._List = new List<ArrangementOne>();
                    this._List.Add(one);
                }
            }
            /// <summary>
            /// Finalise Tick list
            /// </summary>
            public void Finalise()
            {
                if (this._IsFinalised)
                    throw new GraphLibCodeException("Nelze provést finalizaci objektu ArrangementSet více než jedenkrát.");

                if (this._List == null)
                    throw new GraphLibCodeException("Nelze provést finalizaci objektu ArrangementSet, pokud neobsahuje alespoň jeden prvek ArrangementOne.");

                ArrangementOne.SortList(this._List);
                this.Items = this._List.ToArray();
                this._List = null;
                this._IsFinalised = true;
            }
            /// <summary>
            /// Contain true, when this set is empty (does not contains any ArrangementOne)
            /// </summary>
            public bool IsEmpty { get { return (this.Items == null || this.Items.Length == 0); } }
            /// <summary>
            /// Contain true after finalising this instance
            /// </summary>
            public bool IsFinalised { get { return this._IsFinalised; } }
            /// <summary>
            /// Main axis
            /// </summary>
            public BaseAxis<TTick, TSize, TValue> Axis { get; private set; }
            /// <summary>
            /// List of individual arrangements
            /// </summary>
            protected ArrangementOne[] Items { get; private set; }
            /// <summary>
            /// Working List of ArrangementOne, item is added in method AddOne(), is cleared during Finalise
            /// </summary>
            private List<ArrangementOne> _List;
            /// <summary>
            /// Contain true after finalising this instance
            /// </summary>
            private bool _IsFinalised;
            /// <summary>
            /// Select best ArrangementSet for specified scale (scale = number of logical units (seconds, milimeters) per one pixel).
            /// </summary>
            /// <param name="scale"></param>
            /// <returns></returns>
            internal ArrangementOne SelectSetForScale(decimal scale)
            {
                if (!this._IsFinalised)
                    throw new GraphLibCodeException("Není možno pracovat s objektem ArrangementSet (provést SelectSetForScale()) před jeho finalizací.");

                decimal distance = this.SubTitleDistance;
                distance = (distance < 25m ? 25m : (distance > 250m ? 250m : distance));
                ArrangementOne set = this.Items.FirstOrDefault(s => s.IsValidForScale(scale, distance));
                if (set == null)
                    set = this.Items[this.Items.Length - 1];
                return set;
            }
            /// <summary>
            /// Distance (in pixel) of two ticks of type SubTitle, for which will be selected specific ArrangementOne for specified Scale (in method SelectSetForScale()).
            /// Default is 65 pixel. Values lower than 25 or greater than 250 will be ignored.
            /// </summary>
            public decimal SubTitleDistance { get; set; }
        }
        /// <summary>
        /// One TimeAxis arrangement (set of four ticks = ArrangementItem)
        /// </summary>
        protected class ArrangementOne
        {
            /// <summary>
            /// Create one Arrangement = definitions of all Ticks for one scale
            /// </summary>
            /// <param name="pixelSize">Distance for one pixel. One pixel tick is not drawn, but any Value can be Rounded to Pixel, for example for ToolTip or for InteractiveMove.</param>
            /// <param name="stdTickSize">Distance for one standard Tick, as one milimeter on standard plastic ruler.</param>
            /// <param name="bigTickSize">Distance for one big Tick, as 5-milimeter on standard plastic ruler.</param>
            /// <param name="stdLabelSize">Distance for one standard label Tick, as 1-centimeter on standard plastic ruler.</param>
            /// <param name="stdLabelFormat">Format string for formatting Value on standard label Tick.</param>
            /// <param name="bigLabelSize">Distance for one big label Tick, as 10-centimeter on standard plastic ruler.</param>
            /// <param name="bigLabelFormat">Format string for formatting Value on big label Tick.</param>
            /// <param name="outerLabelFormat">Format string for formatting Value on outer labels (Begin and End of Axis).</param>
            /// <param name="axis">Owner Axis</param>
            public ArrangementOne(TSize pixelSize, TSize stdTickSize, TSize bigTickSize, TSize stdLabelSize, string stdLabelFormat, TSize bigLabelSize, string bigLabelFormat, string outerLabelFormat, BaseAxis<TTick, TSize, TValue> axis)
            {
                this.OrderId = 0;
                this.Axis = axis;
                this.PixelItem = new ArrangementItem(AxisTickType.Pixel, pixelSize, null, this);
                this.StdTickItem = new ArrangementItem(AxisTickType.StdTick, stdTickSize, null, this);
                this.BigTickItem = new ArrangementItem(AxisTickType.BigTick, bigTickSize, null, this);
                this.StdLabelItem = new ArrangementItem(AxisTickType.StdLabel, stdLabelSize, stdLabelFormat, this);
                this.BigLabelItem = new ArrangementItem(AxisTickType.BigLabel, bigLabelSize, bigLabelFormat, this);
                this.OuterLabelItem = new ArrangementItem(AxisTickType.OuterLabel, default(TSize), outerLabelFormat, this);
                this.AxisCycle = null;
                this.SelectDistanceRatio = 1m;
            }
            /// <summary>
            /// Create one Arrangement = definitions of all Ticks for one scale
            /// </summary>
            /// <param name="pixelSize">Distance for one pixel. One pixel tick is not drawn, but any Value can be Rounded to Pixel, for example for ToolTip or for InteractiveMove.</param>
            /// <param name="stdTickSize">Distance for one standard Tick, as one milimeter on standard plastic ruler.</param>
            /// <param name="bigTickSize">Distance for one big Tick, as 5-milimeter on standard plastic ruler.</param>
            /// <param name="stdLabelSize">Distance for one standard label Tick, as 1-centimeter on standard plastic ruler.</param>
            /// <param name="stdLabelFormat">Format string for formatting Value on standard label Tick.</param>
            /// <param name="bigLabelSize">Distance for one big label Tick, as 10-centimeter on standard plastic ruler.</param>
            /// <param name="bigLabelFormat">Format string for formatting Value on big label Tick.</param>
            /// <param name="outerLabelFormat">Format string for formatting Value on outer labels (Begin and End of Axis).</param>
            /// <param name="axisCycle">Any string information for this arrangement (special fo Time axis: Month, Week, and so on).</param>
            /// <param name="axis">Owner Axis</param>
            public ArrangementOne(TSize pixelSize, TSize stdTickSize, TSize bigTickSize, TSize stdLabelSize, string stdLabelFormat, TSize bigLabelSize, string bigLabelFormat, string outerLabelFormat, string axisCycle, BaseAxis<TTick, TSize, TValue> axis)
                : this(pixelSize, stdTickSize, bigTickSize, stdLabelSize, stdLabelFormat, bigLabelSize, bigLabelFormat, outerLabelFormat, axis)
            {
                this.AxisCycle = axisCycle;
            }
            /// <summary>
            /// Create one Arrangement = definitions of all Ticks for one scale
            /// </summary>
            /// <param name="pixelSize">Distance for one pixel. One pixel tick is not drawn, but any Value can be Rounded to Pixel, for example for ToolTip or for InteractiveMove.</param>
            /// <param name="stdTickSize">Distance for one standard Tick, as one milimeter on standard plastic ruler.</param>
            /// <param name="bigTickSize">Distance for one big Tick, as 5-milimeter on standard plastic ruler.</param>
            /// <param name="stdLabelSize">Distance for one standard label Tick, as 1-centimeter on standard plastic ruler.</param>
            /// <param name="stdLabelFormat">Format string for formatting Value on standard label Tick.</param>
            /// <param name="bigLabelSize">Distance for one big label Tick, as 10-centimeter on standard plastic ruler.</param>
            /// <param name="bigLabelFormat">Format string for formatting Value on big label Tick.</param>
            /// <param name="outerLabelFormat">Format string for formatting Value on outer labels (Begin and End of Axis).</param>
            /// <param name="axis">Owner Axis</param>
            /// <param name="selectDistanceRatio">Koeficient vyjadřující velikost popisku StdLabel oproti normálu. Normál = 1, kratší popisky mají hodnotu menší než 1.</param>
            public ArrangementOne(TSize pixelSize, TSize stdTickSize, TSize bigTickSize, TSize stdLabelSize, string stdLabelFormat, TSize bigLabelSize, string bigLabelFormat, string outerLabelFormat, BaseAxis<TTick, TSize, TValue> axis, decimal selectDistanceRatio)
                : this(pixelSize, stdTickSize, bigTickSize, stdLabelSize, stdLabelFormat, bigLabelSize, bigLabelFormat, outerLabelFormat, axis)
            {
                this.SelectDistanceRatio = selectDistanceRatio;
            }
            /// <summary>
            /// Create one Arrangement = definitions of all Ticks for one scale
            /// </summary>
            /// <param name="pixelSize">Distance for one pixel. One pixel tick is not drawn, but any Value can be Rounded to Pixel, for example for ToolTip or for InteractiveMove.</param>
            /// <param name="stdTickSize">Distance for one standard Tick, as one milimeter on standard plastic ruler.</param>
            /// <param name="bigTickSize">Distance for one big Tick, as 5-milimeter on standard plastic ruler.</param>
            /// <param name="stdLabelSize">Distance for one standard label Tick, as 1-centimeter on standard plastic ruler.</param>
            /// <param name="stdLabelFormat">Format string for formatting Value on standard label Tick.</param>
            /// <param name="bigLabelSize">Distance for one big label Tick, as 10-centimeter on standard plastic ruler.</param>
            /// <param name="bigLabelFormat">Format string for formatting Value on big label Tick.</param>
            /// <param name="outerLabelFormat">Format string for formatting Value on outer labels (Begin and End of Axis).</param>
            /// <param name="axisCycle">Any string information for this arrangement (special fo Time axis: Month, Week, and so on).</param>
            /// <param name="axis">Owner Axis</param>
            /// <param name="selectDistanceRatio">Koeficient vyjadřující velikost popisku StdLabel oproti normálu. Normál = 1, kratší popisky mají hodnotu menší než 1.</param>
            public ArrangementOne(TSize pixelSize, TSize stdTickSize, TSize bigTickSize, TSize stdLabelSize, string stdLabelFormat, TSize bigLabelSize, string bigLabelFormat, string outerLabelFormat, string axisCycle, BaseAxis<TTick, TSize, TValue> axis, decimal selectDistanceRatio)
                : this(pixelSize, stdTickSize, bigTickSize, stdLabelSize, stdLabelFormat, bigLabelSize, bigLabelFormat, outerLabelFormat, axis)
            {
                this.AxisCycle = axisCycle;
                this.SelectDistanceRatio = selectDistanceRatio;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "Arrangement: " + this.StdLabelItem.ToString() + " (" + this.StdTickItem.ToString() + ")";
            }
            /// <summary>
            /// Textová identifikace do záhlaví osy
            /// </summary>
            public string TextId
            {
                get
                {
                    string text = this.StdLabelItem.Interval.ToString();
                    if (this.SelectDistanceRatio > 0m && this.SelectDistanceRatio != 1m)
                        text = text + " [" + Math.Round(this.SelectDistanceRatio, 2).ToString("##0.00") + "]";
                    return text;
                }
            }
            /// <summary>
            /// ID number in ArrangementSet, after Sort by PixelTickItem.Interval, beginning with 1 (not 0).
            /// </summary>
            public int OrderId { get; private set; }
            /// <summary>
            /// Main axis
            /// </summary>
            public BaseAxis<TTick, TSize, TValue> Axis { get; private set; }
            /// <summary>
            /// Nonvisible ticks, exists only for round time to "pixel" coordinates.
            /// </summary>
            public ArrangementItem PixelItem { get; private set; }
            /// <summary>
            /// Standard non-numbered tick, as 1 mm on ruler
            /// </summary>
            public ArrangementItem StdTickItem { get; private set; }
            /// <summary>
            /// Significant non-numbered tick, as 5 mm on ruler
            /// </summary>
            public ArrangementItem BigTickItem { get; private set; }
            /// <summary>
            /// Standard numbered tick, as 1 cm on ruler
            /// </summary>
            public ArrangementItem StdLabelItem { get; private set; }
            /// <summary>
            /// Big tick, as 10 cm on ruler
            /// </summary>
            public ArrangementItem BigLabelItem { get; private set; }
            /// <summary>
            /// Initial title on begin (and end) of axis
            /// </summary>
            public ArrangementItem OuterLabelItem { get; private set; }
            /// <summary>
            /// Cycle of axis, select day for Title item
            /// </summary>
            public string AxisCycle { get; private set; }
            /// <summary>
            /// Ratio aplikované tímto aranžmá při vyhodnocování jeho vhodnosti pro aktuální měřítko.
            /// Tato hodnota je aplikována na vypočítanou vzdálenost v pixelech pro interval <see cref="StdLabelItem"/>, hodnota <see cref="ArrangementItem.Interval"/>.
            /// Hodnota vyjadřuje, že běžný StdLabel tohoto aranžmá je menší než běžně (když <see cref="SelectDistanceRatio"/> je menší než 1) 
            /// anebo že běžný StdLabel je delší (<see cref="SelectDistanceRatio"/> je větší než 1).
            /// default = 1.0.
            /// </summary>
            public decimal SelectDistanceRatio { get; private set; }
            /// <summary>
            /// Return distance between two tick of SubTitle type for specified scale (scale = number of unit of TSize per one pixel).
            /// </summary>
            /// <param name="scale"></param>
            /// <param name="distance"></param>
            /// <returns></returns>
            internal bool IsValidForScale(decimal scale, decimal distance)
            {
                TSize interval = this.StdLabelItem.Interval;
                decimal pixels = this.StdLabelItem.GetPixelsForScale(scale);
                if (this.SelectDistanceRatio > 0m) distance = this.SelectDistanceRatio * distance;
                return (pixels >= distance);
            }
            /// <summary>
            /// Returns specified Tick value, rounded to specified Tick
            /// </summary>
            /// <param name="tick"></param>
            /// <param name="tickType"></param>
            /// <returns></returns>
            internal TTick RoundValueToTick(TTick tick, AxisTickType tickType)
            {
                ArrangementItem item = this.GetArrangementItemForTick(tickType);
                return (item != null ? item.RoundTickValue(tick) : tick);
            }
            /// <summary>
            /// Add tick for begin and end of timerange
            /// </summary>
            /// <param name="tickDict"></param>
            internal void AddInitialTicks(Dictionary<TTick, BaseTick<TTick>> tickDict)
            {
                this.OuterLabelItem.AddInitialTicks(tickDict);
            }
            /// <summary>
            /// Prepare Ticks for specified TickType to tick Dictionary
            /// </summary>
            /// <param name="tickType"></param>
            /// <param name="tickDict"></param>
            /// <param name="lastTickDict"></param>
            /// <param name="currentPixelOffset"></param>
            internal void CalculateTicksLine(AxisTickType tickType, Dictionary<TTick, BaseTick<TTick>> tickDict, Dictionary<TTick, BaseTick<TTick>> lastTickDict, ref int? currentPixelOffset)
            {
                ArrangementItem item = this.GetArrangementItemForTick(tickType);
                if (item != null)
                    item.CalculateTicksLine(tickDict, lastTickDict, ref currentPixelOffset);
            }
            /// <summary>
            /// Returns instance of ArrangementItem for specified Tick type
            /// </summary>
            /// <param name="tickType"></param>
            /// <returns></returns>
            internal ArrangementItem GetArrangementItemForTick(AxisTickType tickType)
            {
                switch (tickType)
                {
                    case AxisTickType.Pixel: return this.PixelItem;
                    case AxisTickType.StdTick: return this.StdTickItem;
                    case AxisTickType.BigTick: return this.BigTickItem;
                    case AxisTickType.StdLabel: return this.StdLabelItem;
                    case AxisTickType.BigLabel: return this.BigLabelItem;
                }
                return null;
            }
            /// <summary>
            /// Sort list in ascending order.
            /// Fill OrderId property by order.
            /// </summary>
            /// <param name="list"></param>
            public static void SortList(List<ArrangementOne> list)
            {
                if (list == null || list.Count == 0) return;

                if (list.Count > 1)
                {
                    TValue helper = list[0].Axis.Value;
                    list.Sort((a, b) => helper.CompareSize(a.StdLabelItem.Interval, b.StdLabelItem.Interval));
                }

                int orderId = 0;
                foreach (ArrangementOne one in list)
                    one.OrderId = ++orderId;
            }
            /// <summary>
            /// Returns true, when instance a and b is equal (booth is null or booth is not null and have equal OrderId).
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static bool IsEqual(ArrangementOne a, ArrangementOne b)
            {
                bool an = (a == null);
                bool bn = (b == null);
                if (an && bn) return true;
                if (an || bn) return false;
                return (a.OrderId == b.OrderId);
            }
        }
        /// <summary>
        /// Specification of one tick on one TimeAxis arrangement: interval, time format string, support for calculations
        /// </summary>
        protected class ArrangementItem
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="tickType"></param>
            /// <param name="interval"></param>
            /// <param name="textFormat"></param>
            /// <param name="owner"></param>
            public ArrangementItem(AxisTickType tickType, TSize interval, string textFormat, ArrangementOne owner)
            {
                this.Owner = owner;
                this.TickType = tickType;
                this.Interval = interval;
                decimal? unitSize = this.Axis.GetAxisUnits(interval);
                this._UnitSize = (unitSize.HasValue && unitSize.Value > 0m ? unitSize.Value : 0m);
                this.TextFormat = textFormat;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return AxisTickToShort(this.TickType) + ": " + this.Interval.ToString();
            }
            private decimal _UnitSize;
            /// <summary>
            /// Main axis
            /// </summary>
            public BaseAxis<TTick, TSize, TValue> Axis { get { return this.Owner.Axis; } }
            /// <summary>
            /// Owner of this one set = ArrangementSet, set of arrangements of four TickType
            /// </summary>
            public ArrangementOne Owner { get; private set; }
            /// <summary>
            /// Type of this ticks
            /// </summary>
            public AxisTickType TickType { get; private set; }
            /// <summary>
            /// Interval between ticks
            /// </summary>
            public TSize Interval { get; private set; }
            /// <summary>
            /// User string format for display value of this tick on axis
            /// </summary>
            public string TextFormat { get; private set; }
            /// <summary>
            /// Return distance between two tick (in pixels) of this type for specified scale (scale = number of units of Value per one pixel).
            /// </summary>
            /// <param name="scale"></param>
            /// <returns></returns>
            internal decimal GetPixelsForScale(decimal scale)
            {
                // Scale = Units / Pixel;        | * Pixel / Scale
                // Pixel = Units / Scale;
                return this._UnitSize / scale;
            }
            /// <summary>
            /// Standard length of tick between 0 and 1. 
            /// Title has length = 1.00, SubTitle = 0.90, SignificantTick = 0.75, RegularTick = 0.60.
            /// </summary>
            public decimal TickSize
            {
                get
                {
                    if (!this._TickLength.HasValue)
                    {
                        switch (this.TickType)
                        {
                            case AxisTickType.OuterLabel:
                                this._TickLength = 2.00m;
                                break;
                            case AxisTickType.BigLabel:
                                this._TickLength = 1.00m;
                                break;
                            case AxisTickType.StdLabel:
                                this._TickLength = 0.90m;
                                break;
                            case AxisTickType.BigTick:
                                this._TickLength = 0.75m;
                                break;
                            case AxisTickType.StdTick:
                                this._TickLength = 0.60m;
                                break;
                            default:
                                this._TickLength = 0.50m;
                                break;
                        }
                    }
                    return this._TickLength.Value;
                }
            }
            private decimal? _TickLength;
            /// <summary>
            /// Returns specified value rounded to this tick interval (with RoundMode.Math).
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            internal TTick RoundTickValue(TTick value)
            {
                return this.Axis.RoundTickToInterval(value, this.Interval, RoundMode.Math);
            }
            /// <summary>
            /// Add tick for begin and end of timerange
            /// </summary>
            /// <param name="tickDict"></param>
            internal void AddInitialTicks(Dictionary<TTick, BaseTick<TTick>> tickDict)
            {
                TValue value = this.Axis.Value;

                string textBegin = this.GetTickText(value.Begin);
                this.AddOneTick(tickDict, value.Begin, textBegin, AxisTickAlignment.Begin);

                string textEnd = this.GetTickText(value.End);
                if (textEnd != textBegin)
                    this.AddOneTick(tickDict, value.End, textEnd, AxisTickAlignment.End);
            }
            /// <summary>
            /// Uloží do předané Dictionary všechny ticky pro this položku Arrangement (=jedna řada ticků).
            /// Vygeneruje Ticky pro celý aktuální rozsah osy (this.Axis.Value), jen pro ty Ticky, jejichž hodnota dosud v Dictionary není obsažena.
            /// Při správném postupu se tak osa naplní od Ticků největších (Outer, Title) až po nejmenší (Regular) Ticky.
            /// Tato varianta může využívat informace o dřívějších pozicích Ticků v Dictionary lastTickDict 
            /// a o posunu (offsetu) dřívějších a aktuálních Ticků v ref currentPixelOffset.
            /// </summary>
            /// <param name="tickDict"></param>
            /// <param name="lastTickDict"></param>
            /// <param name="currentPixelOffset"></param>
            internal void CalculateTicksLine(Dictionary<TTick, BaseTick<TTick>> tickDict, Dictionary<TTick, BaseTick<TTick>> lastTickDict, ref int? currentPixelOffset)
            {
                TValue value = this.Axis.Value;
                decimal? unitSize = this.Axis.GetAxisUnits(value.Size);
                if (!unitSize.HasValue || unitSize.Value <= 0m) return;
                decimal? tickSize = this.Axis.GetAxisUnits(this.Interval);
                if (!tickSize.HasValue || tickSize.Value <= 0m) return;

                // První hodnota na ose:
                TTick tick = this.Axis.RoundFirstTickForArrangement(value.Begin, this);

                // Dokud pozice Ticku leží uvnitř aktuálního rozsahu osy:
                while (value.Contains(tick))
                {
                    string text = this.GetTickText(tick);
                    this.AddOneTick(tickDict, tick, text, AxisTickAlignment.Center, lastTickDict, ref currentPixelOffset);

                    // Jdeme na další Tick:
                    TTick tickNext = this.Axis.RoundNextTickForArrangement(value.Add(tick, this.Interval), this);

                    // Zabezpečení (překročení počtu ticků celkem, nebo žádný pokrok):
                    if (tickDict.Count >= 1000 || this.Axis._ValueHelper.CompareByEdge(tick, tickNext) == 0) break;

                    // A půjdeme dál s novým tickem:
                    tick = tickNext;
                }
            }
            /// <summary>
            /// Vrátí text pro daný Tick.
            /// Pokud je zadán formátovací string (this.TextFormat), zavolá se abstract metoda this.Axis.GetTickText(tick, this.TextFormat).
            /// Jinak se vrátí prostý tick.ToString().
            /// </summary>
            /// <param name="tick"></param>
            /// <returns></returns>
            private string GetTickText(TTick tick)
            {
                return (this.TextFormat != null ? this.Axis.GetTickText(tick, this.TextFormat) : tick.ToString());
            }
            /// <summary>
            /// Zajistí přidání jednoho Ticku do dané Dictionary.
            /// Tato varianta metody nepracuje s pamětí předchozích pozic (lastTickDict a currentPixelOffset).
            /// </summary>
            /// <param name="tickDict"></param>
            /// <param name="value"></param>
            /// <param name="text"></param>
            /// <param name="alignment"></param>
            private void AddOneTick(Dictionary<TTick, BaseTick<TTick>> tickDict, TTick value, string text, AxisTickAlignment alignment)
            {
                Dictionary<TTick, BaseTick<TTick>> lastTickDict = null;
                int? currentPixelOffset = null;
                this.AddOneTick(tickDict, value, text, alignment, lastTickDict, ref currentPixelOffset);
            }
            /// <summary>
            /// Zajistí přidání jednoho Ticku do dané Dictionary.
            /// Tato varianta metody pracuje s pamětí předchozích pozic (lastTickDict a currentPixelOffset).
            /// </summary>
            /// <param name="tickDict"></param>
            /// <param name="value"></param>
            /// <param name="text"></param>
            /// <param name="alignment"></param>
            /// <param name="lastTickDict"></param>
            /// <param name="currentPixelOffset"></param>
            private void AddOneTick(Dictionary<TTick, BaseTick<TTick>> tickDict, TTick value, string text, AxisTickAlignment alignment, Dictionary<TTick, BaseTick<TTick>> lastTickDict, ref int? currentPixelOffset)
            {
                if (!tickDict.ContainsKey(value))
                {
                    Int32? pixel = null;
                    BaseTick<TTick> lastTick;
                    if (lastTickDict != null && lastTickDict.TryGetValue(value, out lastTick))
                    {   // Pokud máme k dispozici údaje o Ticku pro tutéž hodnotu (value), jako nyní ukládáme na osu:
                        // Přečteme si její tehdejší pozici v pixelech:
                        int lastPixel = lastTick.RelativePixel;
                        if (!currentPixelOffset.HasValue)
                        {   // Pokud dosud nemáme určen offset = posun mezi předchozími Ticky (předchozí stav osy) a aktuálním stavem osy,
                            //  tak zjistíme aktuální pozici ticku a odvodíme si offset pro následující Ticky:
                            pixel = this.Axis.CalculatePixelLocalForTick(value);
                            currentPixelOffset = pixel - lastPixel;
                        }
                        else
                        {   // Pokud již máme určen offset, tak jej použijeme:
                            pixel = lastPixel + currentPixelOffset.Value;
                        }
                    }
                    
                    if (!pixel.HasValue)
                        // Nemáme pole dřívějších Ticků, anebo v něm nebyl Tick pro aktuální hodnotu (value):
                        //  určíme pozici (pixel) standardně výpočtem na ose:
                        pixel = this.Axis.CalculatePixelLocalForTick(value);

                    if (pixel.HasValue)
                    {
                        BaseTick<TTick> tick = new BaseTick<TTick>(this.TickType, value, pixel.Value, this.TickSize, text, alignment);
                        tickDict.Add(value, tick);
                    }
                }
            }
        }
        internal static string AxisTickToShort(AxisTickType tickType)
        {
            switch (tickType)
            {
                case AxisTickType.None: return "None";
                case AxisTickType.Pixel: return "Pixel";
                case AxisTickType.StdTick: return "Tick";
                case AxisTickType.BigTick: return "BigTick";
                case AxisTickType.StdLabel: return "Label";
                case AxisTickType.BigLabel: return "BigLabel";
                case AxisTickType.OuterLabel: return "Outer";
            }
            return "";
        }
        #endregion
        #endregion
        #region Interaktivita osy - data a metody
        /// <summary>
        /// Interaktivní stav osy
        /// </summary>
        public AxisInteractiveState AxisState { get { return this._AxisState; } }
        /// <summary>
        /// Interaktivní stav osy
        /// </summary>
        protected AxisInteractiveState _AxisState;
        /// <summary>
        /// Called after any interactive change value of State
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            e.ToolTipData = null;
            TValue valueOld = this.Value;
            bool canShift = (this.InteractiveChangeMode.HasFlag(AxisInteractiveChangeMode.Shift));
            bool canZoom = (this.InteractiveChangeMode.HasFlag(AxisInteractiveChangeMode.Zoom));
            bool isKeyCtrl = (e.ModifierKeys == Keys.Control);
            bool isKeyShift = (e.ModifierKeys == Keys.Shift);
            bool isSolved = false;
            switch (e.ChangeState)
            {
                case GInteractiveChangeState.MouseOver:
                    this._AxisState = AxisInteractiveState.MouseOver;
                    this.MouseOverRelativePoint = e.MouseRelativePoint;
                    this.Repaint();
                    e.ToolTipData.InfoText = this._CreateToolTip(e.MouseRelativePoint);
                    this.PrepareToolTip(e);
                    break;
                case GInteractiveChangeState.MouseOverDisabled:
                    this._AxisState = AxisInteractiveState.MouseOver;
                    e.ToolTipData.InfoText = this._CreateToolTip(e.MouseRelativePoint);
                    this.PrepareToolTip(e);
                    break;
                case GInteractiveChangeState.LeftDown:
                    this._AxisState = AxisInteractiveState.MouseOver;
                    this._InteractiveOriginalValue = this.GetValue(valueOld);
                    if ((!isKeyCtrl && canShift) || (isKeyCtrl && canZoom))
                    {   // Může být Shift nebo Zoom:
                        this.MouseOverRelativePoint = null;
                        this.MouseDownRelativePoint = e.MouseRelativePoint;
                        this.Repaint();
                        e.ToolTipData.InfoText = this._CreateToolTip(e.MouseRelativePoint);
                        this.PrepareToolTip(e);
                    }
                    else
                    {   // Nemůže být Shift nebo Zoom:
                        e.ToolTipData.InfoText = this._CreateToolTip(e.MouseRelativePoint);
                        this.PrepareToolTip(e);
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveBegin:
                    if (!isKeyCtrl)
                    {   // Shift:
                        if (canShift)
                        {
                            this._AxisState = AxisInteractiveState.DragMove;
                            this._InteractiveShiftIsActive = true;
                            this._InteractiveShiftOrigin = valueOld.Begin;
                            e.UserDragPoint = this.Bounds.Location;
                            e.RequiredCursorType = SysCursorType.Hand;
                            isSolved = true;
                        }
                    }
                    else
                    {   // Zoom:
                        if (canZoom)
                        {
                            this._AxisState = AxisInteractiveState.DragZoom;
                            this._InteractiveZoomIsActive = true;
                            this._InteractiveZoomOrigin = this.GetValue(valueOld);
                            this._InteractiveZoomCenter = this.CalculateTickForPixelRelative(e.MouseRelativePoint.Value);
                            e.UserDragPoint = this.Bounds.Location;
                            e.RequiredCursorType = ((this.OrientationDraw == System.Windows.Forms.Orientation.Horizontal) ? SysCursorType.SizeWE : SysCursorType.SizeNS);
                            isSolved = true;
                        }
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveStep:
                    if (e.UserDragPoint.HasValue)
                    {
                        Point shiftPixel = this.Bounds.Location.Sub(e.UserDragPoint.Value);                            // Distance of shift in number of pixels (as Point {X:Y}, in booth direction): positive value = left/up, negative value = right/down
                        int distancePixel = this.GetShiftByOrientation(shiftPixel);                                    // Distance of shift in number of pixels (as Integer) by OrientationCurrent
                        TValue valueNew = this.Value;
                        string addToolTip = "";
                        string eol = Environment.NewLine;
                        if (this._InteractiveShiftIsActive)
                        {   // Shift:
                            TSize shiftSize = this.CalculateTSizeFromPixelDistance(distancePixel);                     // TSize for current interactive shift
                            TTick shiftBegin = this._ValueHelper.Add(this._InteractiveShiftOrigin, shiftSize);         // Shifted Begin = Original Begin + TSize(shift)
                            if (this.ArrangementCurrent != null)
                                shiftBegin = this.ArrangementCurrent.RoundValueToTick(shiftBegin, AxisTickType.Pixel); // ... Begin, Rounded to PixelTick
                            TValue shiftValue = this.GetValueSize(shiftBegin, valueOld.Size);                          // New Value = Shifted Begin + original Size
                            shiftValue = this.AlignValue(shiftValue);
                            valueNew = shiftValue;

                            if (shiftValue != this._Value)
                            {
                                this.SetValue(shiftValue, ProcessAction.PrepareInnerItems | ProcessAction.CallDraw | ProcessAction.CallChangedEvents | ProcessAction.CallSynchronizeSlave, EventSourceType.InteractiveChanging | EventSourceType.InteractiveChanged | EventSourceType.ValueChange);
                                this.Repaint();
                                addToolTip = eol + "ShiftOrigin = " + this._InteractiveShiftOrigin.ToString() + "; ShiftSize = " + shiftSize.ToString() + "; NewBegin = " + shiftBegin.ToString() + "; NewValue = " + shiftValue.ToString();
                            }
                            if (this.MouseDownRelativePoint.HasValue)
                                this.MouseDownRelativePoint = e.MouseRelativePoint.Value;
                            isSolved = true;
                        }
                        else if (this._InteractiveZoomIsActive)
                        {   // Zoom:
                            int silentZone = 5;                                                                        // Silent zone = 5 pixel with ratio = 1
                            distancePixel = (distancePixel >= silentZone ? distancePixel - silentZone : (distancePixel < -silentZone ? distancePixel + silentZone : 0));
                            TValue zoomValue = null;
                            if (distancePixel == 0)
                            {   // Original Value, without a change:
                                zoomValue = this.GetValue(this._InteractiveZoomOrigin);
                                addToolTip = eol + "Silent: ShiftPixel = " + shiftPixel.ToString() + "; DistancePixel = " + distancePixel.ToString() + "; ZoomRatio = 0; ZoomValue = " + zoomValue.ToString();
                            }
                            else
                            {   // Zoomed Value:
                                double zoomRatio = Math.Pow(2.0D, ((double)distancePixel / 40d));                      // Each 40 pixels double / half scale; Coefficient to zoom of original SizeRange
                                zoomValue = this.CalculateValueByRatio(this.GetValue(this._InteractiveZoomOrigin), this._InteractiveZoomCenter, zoomRatio);
                                addToolTip = eol + "Zoom: ShiftPixel = " + shiftPixel.ToString() + "; DistancePixel = " + distancePixel.ToString() + "; ZoomRatio = " + zoomRatio.ToString() + "; ZoomValue = " + zoomValue.ToString();
                            }
                            zoomValue = this.AlignValue(zoomValue);
                            valueNew = zoomValue;

                            addToolTip += eol + "; ZoomAlignedValue = " + zoomValue.ToString();
                            if (zoomValue != this._Value)
                            {
                                this.SetValue(zoomValue, ProcessAction.RecalcScale | ProcessAction.RecalcInnerData | ProcessAction.PrepareInnerItems | ProcessAction.CallDraw | ProcessAction.CallChangedEvents | ProcessAction.CallSynchronizeSlave, EventSourceType.InteractiveChanging | EventSourceType.InteractiveChanged | EventSourceType.ValueChange);
                                this.Repaint();
                                addToolTip += "; SetValue()";
                            }
                            isSolved = true;
                        }
                        if (isSolved)
                        {
                            e.ToolTipData.InfoText = this._CreateToolTip(valueNew); //  + addToolTip;
                            this.PrepareToolTip(e);
                        }
                    }
                    if (!isSolved)
                    {   // Neproběhl ani Shift, ani Drag => budu se chovat, jako by to bylo MouseOver:
                        this.MouseOverRelativePoint = e.MouseRelativePoint;
                        this.Repaint();
                        e.ToolTipData.InfoText = this._CreateToolTip(e.MouseRelativePoint);
                        this.PrepareToolTip(e);
                    }
                    break;
                case GInteractiveChangeState.LeftDragMoveCancel:
                    TValue originalValue = this._InteractiveOriginalValue;
                    this.SetValue(originalValue, ProcessAction.RecalcScale | ProcessAction.RecalcInnerData | ProcessAction.PrepareInnerItems | ProcessAction.CallDraw | ProcessAction.CallChangedEvents | ProcessAction.CallSynchronizeSlave, EventSourceType.InteractiveChanging | EventSourceType.InteractiveChanged | EventSourceType.ValueChange);
                    this.Repaint();
                    break;
                case GInteractiveChangeState.LeftDragMoveDone:
                    this._AxisState = AxisInteractiveState.MouseOver;
                    break;
                case GInteractiveChangeState.LeftDragMoveEnd:
                    this._AxisState = AxisInteractiveState.MouseOver;
                    this._InteractiveShiftIsActive = false;
                    this._InteractiveZoomIsActive = false;
                    e.RequiredCursorType = SysCursorType.Default;
                    e.ToolTipData.InfoText = this._CreateToolTip(e.MouseRelativePoint);
                    this.PrepareToolTip(e);
                    break;

                case GInteractiveChangeState.LeftUp:
                    this._AxisState = AxisInteractiveState.MouseOver;
                    this.MouseDownRelativePoint = null;
                    this.MouseOverRelativePoint = e.MouseRelativePoint;
                    this.Repaint();
                    e.ToolTipData.InfoText = this._CreateToolTip(e.MouseRelativePoint);
                    this.PrepareToolTip(e);
                    break;

                case GInteractiveChangeState.LeftDoubleClick:
                    if (this.ValueLimit != null && this.ValueLimit.IsFilled && this.ValueLimit != this.Value)
                    {
                        this.SetValue(this.ValueLimit, ProcessAction.RecalcScale | ProcessAction.RecalcInnerData | ProcessAction.PrepareInnerItems | ProcessAction.CallDraw | ProcessAction.CallChangedEvents | ProcessAction.CallSynchronizeSlave, EventSourceType.InteractiveChanged | EventSourceType.ValueChange);
                        this.Repaint();
                    }
                    break;

                case GInteractiveChangeState.WheelUp:
                case GInteractiveChangeState.WheelDown:
                    TValue value = this.Value;
                    bool wheelUp = (e.ChangeState == GInteractiveChangeState.WheelUp);
                    if (!isKeyCtrl)
                    {   // Myší kolečko bez Ctrl => samotné nebo se Shiftem = posun hodnoty:
                        if (canShift)
                        {   // Posun hodnoty: filozoficky to beru jako Scroll v odkumentu: 
                            //  - když se točí kolečkem dolů, tak dokument se posouvá "dolů" = ukazuje další řádky a stránky, pokračování děje
                            //  - tak i (časová) osa bude při točení dolů (WheelDown) ukazovat pokračování děje = vyšší hodnoty DateTime:
                            this._AxisState = AxisInteractiveState.DragMove;
                            double shiftWeight = (isKeyShift ? 0.02d : 0.06d);           // Se Shiftem je to 3x jemnější
                            double shiftRatio = shiftWeight * (wheelUp ? -1d : 1d);
                            TValue shiftValue = this.CalculateValueByShift(valueOld, shiftRatio, AxisTickType.Pixel);
                            shiftValue = this.AlignValue(shiftValue);
                            if (shiftValue != this._Value)
                            {
                                this.SetValue(shiftValue, ProcessAction.PrepareInnerItems | ProcessAction.CallDraw | ProcessAction.CallChangedEvents | ProcessAction.CallSynchronizeSlave, EventSourceType.InteractiveChanged | EventSourceType.ValueChange);
                                this.Repaint();
                                e.ToolTipData.InfoText = this._CreateToolTip(valueOld);
                                this.PrepareToolTip(e);
                            }
                            this._AxisState = AxisInteractiveState.MouseOver;
                        }
                    }
                    else
                    {   // Myší kolečko s Ctrl => Zoom hodnoty se středem pod myší:
                        if (canZoom)
                        {
                            this._AxisState = AxisInteractiveState.DragZoom;
                            double zoomWeight = (isKeyShift ? 1.05d : 1.15d);            // Se Shiftem je to 3x jemnější
                            double zoomRatio = (wheelUp ? 1d / zoomWeight : zoomWeight);
                            TTick zoomCenter = this.CalculateTickForPixelRelative(e.MouseRelativePoint.Value);
                            TValue zoomValue = this.CalculateValueByRatio(valueOld, zoomCenter, zoomRatio);
                            zoomValue = this.AlignValue(zoomValue);
                            if (zoomValue != this._Value)
                            {
                                this.SetValue(zoomValue, ProcessAction.RecalcScale | ProcessAction.RecalcInnerData | ProcessAction.PrepareInnerItems | ProcessAction.CallDraw | ProcessAction.CallChangedEvents | ProcessAction.CallSynchronizeSlave, EventSourceType.InteractiveChanged | EventSourceType.ValueChange);
                                this.Repaint();
                                e.ToolTipData.InfoText = this._CreateToolTip(valueOld);
                                this.PrepareToolTip(e);
                            }
                            this._AxisState = AxisInteractiveState.MouseOver;
                        }
                    }
                    e.ActionIsSolved = true;
                    break;

                case GInteractiveChangeState.MouseLeave:
                    this._AxisState = AxisInteractiveState.None;
                    this.MouseOverRelativePoint = null;
                    this.Repaint();
                    break;
            }
        }
        /// <summary>
        /// Create and return text for tooltip for specified relative point
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <returns></returns>
        private string _CreateToolTip(Point? relativePoint)
        {
            if (!relativePoint.HasValue) return null;
            if (!this.IsValid) return null;
            TTick value = this.CalculateTickForPixelRelative(relativePoint.Value, AxisTickType.Pixel);
            string text = this.PrepareToolTipText(value);
            return text;
        }
        /// <summary>
        /// Create and return text for tooltip for specified relative point
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string _CreateToolTip(TValue value)
        {
            if (value == null) return null;
            TValue rounded = this.GetValue(this.ArrangementCurrent.RoundValueToTick(value.Begin, AxisTickType.Pixel), this.ArrangementCurrent.RoundValueToTick(value.End, AxisTickType.Pixel));
            return rounded.ToString();
        }
        /// <summary>
        /// Hodnota na ose v okamžiku, kdy se stiskla myš, před zahájením změn.
        /// Lze ji použít v DragCancel.
        /// </summary>
        private TValue _InteractiveOriginalValue;
        /// <summary>
        /// Value of Axis.Begin at time, where Shift started
        /// </summary>
        private TTick _InteractiveShiftOrigin;
        /// <summary>
        /// Contain true, when Shift action is active.
        /// </summary>
        private bool _InteractiveShiftIsActive = false;
        /// <summary>
        /// Logical point on this Axis where Zoom started
        /// </summary>
        private TTick _InteractiveZoomCenter;
        /// <summary>
        /// Value of Axis (Begin, End), where Zoom started
        /// </summary>
        private TValue _InteractiveZoomOrigin;
        /// <summary>
        /// Contain true, when Zoom action is active.
        /// </summary>
        private bool _InteractiveZoomIsActive = false;
        /// <summary>
        /// Relative coordinates of point, where is Mouse in MouseOver state. 
        /// For visual response in method DrawMousePoint().
        /// </summary>
        protected Point? MouseOverRelativePoint;
        /// <summary>
        /// Relative coordinates of point, where is Mouse in MouseDown state. 
        /// For visual response in method DrawMousePoint().
        /// </summary>
        protected Point? MouseDownRelativePoint;
        #endregion
        #region Draw to Graphic
        /// <summary>
        /// Draw current size axis to specified graphic.
        /// Coordinates are this.Area.
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            e.GraphicsClipWith(absoluteVisibleBounds);               // ne absoluteBounds!

            if (this.IsBoundsValid && this.IsVisualValid && this.IsValueValid && !this.IsScaleValid)
                this.CalculateRequiredEntities();

            if (this.IsBoundsValid && this.IsAxisValid && e.DrawLayer == GInteractiveDrawLayer.Standard)
            {
                this.CalculateRequiredEntities();
                Rectangle clip = e.GetClip(absoluteVisibleBounds);
                using (Painter.GraphicsUseText(e.Graphics, clip))
                {
                    this.DrawBackground(e.Graphics, absoluteBounds);
                    this.DrawMousePoint(e.Graphics, absoluteBounds);
                    this.DrawTicks(e.Graphics, absoluteBounds);
                    this.DrawArrangementInfo(e.Graphics, absoluteBounds);
                }
            }
        }
        /// <summary>
        /// Draw background of axis
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="absoluteBounds"></param>
        protected void DrawBackground(Graphics graphics, Rectangle absoluteBounds)
        {
            this.DrawBackground(graphics, absoluteBounds, this.BackColor.Value);
            this.DrawSegments(graphics, absoluteBounds);
        }
        /// <summary>
        /// Vykreslí pozadí pod osou.
        /// Slouží i pro vykreslení segmentů.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="backColor"></param>
        protected void DrawBackground(Graphics graphics, Rectangle absoluteBounds, Color backColor)
        {
            Painter.DrawAxisBackground(graphics, absoluteBounds, this.OrientationDraw, this.Is.Enabled, this.InteractiveState, backColor, this.BackColor3DEffect);
        }
        /// <summary>
        /// Draw Mouse point (a glare ellipse), when point location is stored in MouseOverRelativePoint or MouseDownRelativePoint (when HasValue)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="absoluteBounds"></param>
        protected void DrawMousePoint(Graphics graphics, Rectangle absoluteBounds)
        {
            if (this.MouseOverRelativePoint.HasValue)
            {
                Point mousePoint = absoluteBounds.Location.Add(this.MouseOverRelativePoint.Value);
                Painter.DrawRadiance(graphics, mousePoint, absoluteBounds, Skin.Modifiers.MouseMoveTracking);
            }
            else if (this.MouseDownRelativePoint.HasValue)
            {
                Point mousePoint = absoluteBounds.Location.Add(this.MouseDownRelativePoint.Value);
                Painter.DrawRadiance(graphics, mousePoint, absoluteBounds, Skin.Modifiers.MouseDragTracking);
            }
        }
        /// <summary>
        /// Draw all ticks for this Axis
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="absoluteBounds"></param>
        protected void DrawTicks(Graphics graphics, Rectangle absoluteBounds)
        {
            if (this.TickList == null) return;

            using (BaseAxisTickPainter tickPainter = new BaseAxisTickPainter(absoluteBounds, this.Orientation, false))
            {
                foreach (BaseTick<TTick> tick in this.TickList)
                    tickPainter.DrawTick(graphics, tick);
            }
            return;
        }
        /// <summary>
        /// Draw debug info for this Axis
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="absoluteBounds"></param>
        protected void DrawArrangementInfo(Graphics graphics, Rectangle absoluteBounds)
        {
            if (this.ArrangementCurrent == null) return;

            FontInfo fontInfo = new FontInfo() { FontType = FontSetType.DefaultFont, SizeRatio = 0.75f, Italic = true };
            using (StringFormat sf = new StringFormat(StringFormatFlags.NoClip))
            {
                string text = this.ArrangementCurrent.TextId;
                SizeF size = graphics.MeasureString(text, fontInfo.Font);
                PointF point = new PointF(absoluteBounds.Right - 3, absoluteBounds.Top + 1);
                RectangleF area = size.AlignTo(absoluteBounds, ContentAlignment.TopCenter);
                area.Y += 2;
                graphics.DrawString(text, fontInfo.Font, Skin.Brush(Skin.Axis.TextColorArrangement), area, sf);
            }
        }
        #endregion
        #region Vyvolání eventů (ValueChanged, ValueRangeChanged, ScaleChanged, ScaleRangeChanged, AreaChanged, DrawRequest)
        /// <summary>
        /// Call method OnDataRangeChanged() and event DataRangeChanged
        /// </summary>
        protected void CallValueChanging(TValue oldValue, TValue newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<TValue> args = new GPropertyChangeArgs<TValue>(oldValue, newValue, eventSource);
            this.OnValueChanging(args);
            if (!this.IsSuppressedEvent && this.ValueChanging != null)
                this.ValueChanging(this, args);
        }
        /// <summary>
        /// Occured after change value in this.Visual*Bounds
        /// </summary>
        protected virtual void OnValueChanging(GPropertyChangeArgs<TValue> args) { }
        /// <summary>
        /// Event on this.Value changes
        /// </summary>
        public event GPropertyChangedHandler<TValue> ValueChanging;

        /// <summary>
        /// Call method OnDataRangeChanged() and event DataRangeChanged
        /// </summary>
        protected void CallValueChanged(TValue oldValue, TValue newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<TValue> args = new GPropertyChangeArgs<TValue>(oldValue, newValue, eventSource);
            this.OnValueChanged(args);
            if (!this.IsSuppressedEvent && this.ValueChanged != null)
                this.ValueChanged(this, args);
        }
        /// <summary>
        /// Occured after change value in this.Visual*Bounds
        /// </summary>
        protected virtual void OnValueChanged(GPropertyChangeArgs<TValue> args) { }
        /// <summary>
        /// Event on this.Value changes
        /// </summary>
        public event GPropertyChangedHandler<TValue> ValueChanged;

        /// <summary>
        /// Call method OnValueRangeChanged() and event ValueRangeChanged
        /// </summary>
        protected void CallValueLimitChanged(TValue oldValue, TValue newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<TValue> args = new GPropertyChangeArgs<TValue>(oldValue, newValue, eventSource);
            this.OnValueLimitChanged(args);
            if (!this.IsSuppressedEvent && this.ValueLimitChanged != null)
                this.ValueLimitChanged(this, args);
        }
        /// <summary>
        /// Occured after change ValueRange in this.Visual*Bounds
        /// </summary>
        protected virtual void OnValueLimitChanged(GPropertyChangeArgs<TValue> args) { }
        /// <summary>
        /// Event on this.ValueRange changes
        /// </summary>
        public event GPropertyChangedHandler<TValue> ValueLimitChanged;

        /// <summary>
        /// Call method OnSlaveSynchronize() and event SlaveSynchronize
        /// </summary>
        protected void CallSlaveSynchronize(TValue oldValue, TValue newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<TValue> args = new GPropertyChangeArgs<TValue>(oldValue, newValue, eventSource);
            this.OnSlaveSynchronize(args);
            if (!this.IsSuppressedEvent && this.SlaveSynchronize != null)
                this.SlaveSynchronize(this, args);
        }
        /// <summary>
        /// Occured after change value in this.Visual*Bounds
        /// </summary>
        protected virtual void OnSlaveSynchronize(GPropertyChangeArgs<TValue> args) { }
        /// <summary>
        /// Event on this.Value changes
        /// </summary>
        public event GPropertyChangedHandler<TValue> SlaveSynchronize;

        /// <summary>
        /// Call method OnScaleChanged() and event ScaleChanged
        /// </summary>
        protected void CallScaleChanged(decimal oldValue, decimal newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<decimal> args = new GPropertyChangeArgs<decimal>(oldValue, newValue, eventSource);
            this.OnScaleChanged(args);
            if (!this.IsSuppressedEvent && this.ScaleChanged != null)
                this.ScaleChanged(this, args);
        }
        /// <summary>
        /// Occured after change Scale value
        /// </summary>
        protected virtual void OnScaleChanged(GPropertyChangeArgs<decimal> args) { }
        /// <summary>
        /// Event on this.Scale changes
        /// </summary>
        public event GPropertyChangedHandler<decimal> ScaleChanged;

        /// <summary>
        /// Call method OnScaleRangeChanged() and event ScaleRangeChanged
        /// </summary>
        protected void CallScaleLimitChanged(DecimalNRange oldValue, DecimalNRange newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<DecimalNRange> args = new GPropertyChangeArgs<DecimalNRange>(oldValue, newValue, eventSource);
            this.OnScaleLimitChanged(args);
            if (!this.IsSuppressedEvent && this.ScaleLimitChanged != null)
                this.ScaleLimitChanged(this, args);
        }
        /// <summary>
        /// Occured after change ScaleRange value
        /// </summary>
        protected virtual void OnScaleLimitChanged(GPropertyChangeArgs<DecimalNRange> args) { }
        /// <summary>
        /// Event on this.ScaleRange changes
        /// </summary>
        public event GPropertyChangedHandler<DecimalNRange> ScaleLimitChanged;

        /// <summary>
        /// Call method OnArrangementChanged() and event ArrangementChanged
        /// </summary>
        protected void CallArrangementChanged(ArrangementOne oldArrangement, ArrangementOne newArrangement, EventSourceType eventSource)
        {
            GPropertyChangeArgs<ArrangementOne> args = new GPropertyChangeArgs<ArrangementOne>(oldArrangement, newArrangement, eventSource);
            this.OnArrangementChanged(args);
            if (this.ArrangementChanged != null)
                this.ArrangementChanged(this, args);
        }
        /// <summary>
        /// Occured after change Scale value
        /// </summary>
        protected virtual void OnArrangementChanged(GPropertyChangeArgs<ArrangementOne> args) { }
        /// <summary>
        /// Event on this.Scale changes
        /// </summary>
        protected event GPropertyChangedHandler<ArrangementOne> ArrangementChanged;

        /// <summary>
        /// Call method OnAreaChange() and event AreaChange
        /// </summary>
        protected void CallAreaChanged(Rectangle oldValue, Rectangle newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<Rectangle> args = new GPropertyChangeArgs<Rectangle>(oldValue, newValue, eventSource);
            this.OnAreaChanged(args);
            if (!this.IsSuppressedEvent && this.AreaChanged != null)
                this.AreaChanged(this, args);
        }
        /// <summary>
        /// Occured after change value in this.Visual*Bounds
        /// </summary>
        protected virtual void OnAreaChanged(GPropertyChangeArgs<Rectangle> args) { }
        /// <summary>
        /// Event on this.AreaChange occured
        /// </summary>
        public event GPropertyChangedHandler<Rectangle> AreaChanged;

        /// <summary>
        /// Call method OnAreaChange() and event AreaChange
        /// </summary>
        protected void CallTicksChanged(BaseTick<TTick>[] oldTickList, BaseTick<TTick>[] newTickList, EventSourceType eventSource)
        {
            GPropertyChangeArgs<BaseTick<TTick>[]> args = new GPropertyChangeArgs<BaseTick<TTick>[]>(oldTickList, newTickList, eventSource);
            this.OnTicksChanged(args);
            if (!this.IsSuppressedEvent && this.TicksChanged != null)
                this.TicksChanged(this, args);
        }
        /// <summary>
        /// Occured after change value in this.Visual*Bounds
        /// </summary>
        protected virtual void OnTicksChanged(GPropertyChangeArgs<BaseTick<TTick>[]> args) { }
        /// <summary>
        /// Event on this.AreaChange occured
        /// </summary>
        public event GPropertyChangedHandler<BaseTick<TTick>[]> TicksChanged;

        /// <summary>
        /// Call method OnOrientationChanged() and event OrientationChanged
        /// </summary>
        protected void CallOrientationChanged(AxisOrientation oldValue, AxisOrientation newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<AxisOrientation> args = new GPropertyChangeArgs<AxisOrientation>(oldValue, newValue, eventSource);
            this.OnOrientationChanged(args);
            if (!this.IsSuppressedEvent && this.OrientationChanged != null)
                this.OrientationChanged(this, args);
        }
        /// <summary>
        /// Occured after change Scale value
        /// </summary>
        protected virtual void OnOrientationChanged(GPropertyChangeArgs<AxisOrientation> args) { }
        /// <summary>
        /// Event on this.Scale changes
        /// </summary>
        public event GPropertyChangedHandler<AxisOrientation> OrientationChanged;

        #endregion
        #region Segmenty osy
        /// <summary>
        /// Pole segmentů na této ose
        /// </summary>
        public Segment[] Segments
        {
            get { return this._Segments; }
            set
            {
                this._Segments = value;
                this._ISegmentsCurrent = null;
                this._ISegments = null;
                if (value != null)
                    this._ISegments = value.Select(s => s as ISegment).ToArray();
                this.Repaint();
            }
        }
        /// <summary>
        /// Hodnota vložená do property <see cref="Segments"/>
        /// </summary>
        private Segment[] _Segments;
        /// <summary>
        /// Privátní pole hodnot, vložených sice do property <see cref="Segments"/>, ale plně izolované a převedení na ISegment
        /// </summary>
        private ISegment[] _ISegments;
        /// <summary>
        /// Vrátí přídavné texty do ToolTipu pro danou hodnotu, získané z definic segmentů
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected string GetSegmentsToolTip(TTick value)
        {
            ISegment[] iSegments = this.SegmentsCurrent;
            if (iSegments.Length == 0) return null;
            string toolTip = "";
            foreach (ISegment iSegment in iSegments)
            {
                string text = iSegment.ToolTip;
                if (String.IsNullOrEmpty(text)) continue;
                if (iSegment.ValueRange.Contains(value, false))
                    toolTip = toolTip + Environment.NewLine + text;
            }
            return toolTip;
        }
        /// <summary>
        /// Vykreslí pozadí segmentů
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="absoluteBounds"></param>
        protected void DrawSegments(Graphics graphics, Rectangle absoluteBounds)
        {
            ISegment[] iSegments = this.SegmentsCurrent;
            if (iSegments.Length == 0) return;
            foreach (ISegment iSegment in iSegments)
            {
                if (!iSegment.BackColor.HasValue) continue;
                Rectangle? segmentBounds = CreateSegmentBounds(absoluteBounds, this.Orientation, iSegment.PixelRange, iSegment.HeightRange, iSegment.SizeRange);
                if (segmentBounds.HasValue)
                    this.DrawBackground(graphics, segmentBounds.Value, iSegment.BackColor.Value);
            }
        }
        /// <summary>
        /// Vrátí souřadnice daného segmentu
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <param name="axisOrientation"></param>
        /// <param name="segmentRange"></param>
        /// <param name="heightRange"></param>
        /// <param name="sizeRange"></param>
        /// <returns></returns>
        protected Rectangle? CreateSegmentBounds(Rectangle absoluteBounds, AxisOrientation axisOrientation, Int32Range segmentRange, Int32Range heightRange, DoubleRange sizeRange)
        {
            int x = absoluteBounds.X;
            int w = absoluteBounds.Width;
            int y = absoluteBounds.Y;
            int h = absoluteBounds.Height;
            int b = y + h;
            int sb = 0;
            int ss = 0;
            switch (axisOrientation)
            {
                case AxisOrientation.Top:
                case AxisOrientation.Bottom:
                    CreateSegmentSize(heightRange, sizeRange, y, h, (axisOrientation == AxisOrientation.Top), out sb, out ss);
                    return new Rectangle(x + segmentRange.Begin, sb, segmentRange.Size, ss);
                case AxisOrientation.LeftUp:
                case AxisOrientation.RightUp:
                    CreateSegmentSize(heightRange, sizeRange, x, w, (axisOrientation == AxisOrientation.LeftUp), out sb, out ss);
                    return new Rectangle(sb, b  - segmentRange.End, ss, segmentRange.Size);
                case AxisOrientation.LeftDown:
                case AxisOrientation.RightDown:
                    CreateSegmentSize(heightRange, sizeRange, x, w, (axisOrientation == AxisOrientation.LeftDown), out sb, out ss);
                    return new Rectangle(sb, y + segmentRange.Begin, ss, segmentRange.Size);
            }
            return null;
        }
        /// <summary>
        /// Určí souřadnice segmentu v dimenzi, která nezobrazuje hodnotu; tedy např. na běžné vodorovné časové ose určuje souřadnice v ose Y.
        /// Akceptuje požadavky segmentu <see cref="ISegment.HeightRange"/> a <see cref="ISegment.SizeRange"/>, akceptuje pozici celkové osy v daném směru 
        /// v parametrech (totalBegin) = na vodorovné ose pozice Y celé osy, a (totalSize) = výška osy, 
        /// akceptuje parametr (alignEnd), který říká, že "pozice 0 osy je na dolním/pravém okraji",
        /// a určí out souřadnice segmentu v dané souřadnici (segmentBegin) a (segmentSize).
        /// </summary>
        /// <param name="heightRange"></param>
        /// <param name="sizeRange"></param>
        /// <param name="totalBegin"></param>
        /// <param name="totalSize"></param>
        /// <param name="alignEnd"></param>
        /// <param name="segmentBegin"></param>
        /// <param name="segmentSize"></param>
        protected void CreateSegmentSize(Int32Range heightRange, DoubleRange sizeRange, int totalBegin, int totalSize, bool alignEnd, out int segmentBegin, out int segmentSize)
        {
            segmentBegin = totalBegin;
            segmentSize = totalSize;
            if (sizeRange != null)
            {
                int b = 0;
                int e = totalSize;
                if (heightRange != null)
                {
                    b = heightRange.Begin;
                    e = heightRange.End;
                }
                else if (sizeRange != null)
                {
                    b = (int)Math.Round((sizeRange.Begin * (double)totalSize), 0);
                    e = (int)Math.Round((sizeRange.End * (double)totalSize), 0);
                }
                b = (b < 0 ? 0 : (b > totalSize ? totalSize : b));
                e = (e < b ? b : (e > totalSize ? totalSize : e));

                segmentBegin = (alignEnd ? (totalBegin + totalSize - e) : (totalBegin + b));
                segmentSize = (e - b);
            }
        }
        /// <summary>
        /// Aktuálně viditelné segmenty
        /// </summary>
        protected ISegment[] SegmentsCurrent { get { this.CheckSegmentsCurrent(); return this._ISegmentsCurrent; } }
        private ISegment[] _ISegmentsCurrent;
        /// <summary>
        /// Zajistí platnost dat v poli <see cref="_ISegmentsCurrent"/> na základě dat v poli <see cref="_Segments"/> a aktuálního zobrazeného intervalu
        /// </summary>
        protected void CheckSegmentsCurrent()
        {
            bool isValid = (this._ISegmentsCurrent != null);
            if (isValid && !this.IsEqual(this._SegmentCurrentValue, this._Value))
                isValid = false;
            if (isValid && this._SegmentCurrentSize != this.PixelSize)
                isValid = false;

            if (isValid)
            {
                return;
            }

            List<ISegment> iSegmentList = new List<ISegment>();
            if (this._ISegments != null)
            {
                foreach (ISegment iSegment in this._ISegments)
                {
                    if (iSegment != null && iSegment.PrepareForAxis(this))
                        iSegmentList.Add(iSegment);
                }
            }
            this._SegmentCurrentValue = this.GetValue(this.Value);
            this._SegmentCurrentSize = this.PixelSize;
            this._ISegmentsCurrent = iSegmentList.ToArray();
        }
        private TValue _SegmentCurrentValue;
        private decimal _SegmentCurrentSize;
        #region class Segment + interface ISegment
        /// <summary>
        /// Definice segmentů na ose, segmenty mohou mít odlišnou barvu a/nebo tooltip než základní osa, a tak mohou sdělovat "něco navíc".
        /// </summary>
        /// <remarks>
        /// Třída Segment je vnořená do GBaseAxis kvůli generikám.
        /// </remarks>
        public class Segment : ISegment
        {
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "Segment: " + this.ValueRange + "; PixelRange: " + (this._PixelRange != null ? this._PixelRange.ToString() : "{Null}");
            }
            /// <summary>
            /// Rozmezí hodnot, kde se nachází segment
            /// </summary>
            public TValue ValueRange { get; set; }
            /// <summary>
            /// Barva pozadí v tomto segmentu
            /// </summary>
            public Color? BackColor { get; set; }
            /// <summary>
            /// Rozmezí výšky na ose Y, které bude obarveno barvou <see cref="BackColor"/>, zadané absolutně v pixelech.
            /// Pokud je null, značí celou výšku osy.
            /// Souběh zadání <see cref="SizeRange"/> a <see cref="HeightRange"/>:
            /// Pokud je zadáno <see cref="HeightRange"/>, pak má přednost před zadáním <see cref="SizeRange"/>.
            /// Vyjadřuje prostor na vizuální ose ve směru Y, ve kterém bude zobrazeno obarvení tohoto segmentu.
            /// </summary>
            public Int32Range HeightRange { get; set; }
            /// <summary>
            /// Rozmezí výšky na ose Y, které bude obarveno barvou <see cref="BackColor"/>.
            /// Povolené rozmezí = 0 až 1, což je i defaultní hodnota v případě null, značí celou výšku osy.
            /// Souběh zadání <see cref="SizeRange"/> a <see cref="HeightRange"/>:
            /// Pokud je zadáno <see cref="HeightRange"/>, pak má přednost před zadáním <see cref="SizeRange"/>.
            /// Vyjadřuje prostor na vizuální ose ve směru Y, ve kterém bude zobrazeno obarvení tohoto segmentu.
            /// </summary>
            public DoubleRange SizeRange { get; set; }
            /// <summary>
            /// Text pro Tooltip v daném rozmezí, přidává se pod standardní text ToolTipu
            /// </summary>
            public string ToolTip { get; set; }
            /// <summary>
            /// Aktuální rozmezí v pixelech na aktivní souřadnici osy (vodorovná osa = souřadnice X)
            /// </summary>
            Int32Range ISegment.PixelRange { get { return this._PixelRange; } }
            /// <summary>
            /// Metoda připraví souřadnice do <see cref="ISegment.PixelRange"/> a vrátí true, pokud daný segment má být na aktuální ose zobrazen.
            /// </summary>
            /// <param name="axis"></param>
            /// <returns></returns>
            bool ISegment.PrepareForAxis(BaseAxis<TTick, TSize, TValue> axis)
            {
                bool result = false;
                this._PixelRange = null;
                if (this.ValueRange != null && axis.Value.HasIntersect(this.ValueRange))
                {   // Pokud daná hodnota segmentu (ValueRange) se nachází ve viditelné části osy:
                    decimal? pixelBegin = axis.GetPixelForValue(this.ValueRange.Begin);
                    decimal? pixelEnd = axis.GetPixelForValue(this.ValueRange.End);
                    if (pixelBegin.HasValue && pixelEnd.HasValue)
                    {   // Pokud reálné pixely byly vypočteny:
                        decimal pixelSize = axis.PixelSize;
                        if (pixelEnd.Value > 0m && pixelBegin.Value < pixelSize)
                        {   // Pokud reálné pixely jsou ve viditelné části osy:
                            // Ošetřit hodnoty mimo rozumný rozsah:
                            decimal axisMin = -1000;
                            if (pixelBegin.Value < axisMin) pixelBegin = axisMin;
                            decimal axisMax = pixelSize + 1000m;
                            if (pixelEnd.Value > axisMax) pixelEnd = axisMax;
                            this._PixelRange = new Int32Range((int)Math.Round(pixelBegin.Value, 0), (int)Math.Round(pixelEnd.Value, 0));
                            result = true;
                        }
                    }
                }
                return result;
            }
            private Int32Range _PixelRange;
        }
        /// <summary>
        /// Interface pro interní přístup k funkčním členům třídy Segment
        /// </summary>
        /// <remarks>
        /// Interface ISegment je vnořená do GBaseAxis kvůli generikám.
        /// </remarks>
        protected interface ISegment
        {
            /// <summary>
            /// Rozmezí hodnot, kde se nachází segment
            /// </summary>
            TValue ValueRange { get; }
            /// <summary>
            /// Barva pozadí v tomto segmentu
            /// </summary>
            Color? BackColor { get; }
            /// <summary>
            /// Rozmezí výšky na ose Y, které bude obarveno barvou <see cref="BackColor"/>, zadané absolutně v pixelech.
            /// Pokud je null, značí celou výšku osy.
            /// Souběh zadání <see cref="SizeRange"/> a <see cref="HeightRange"/>:
            /// Pokud je zadáno <see cref="HeightRange"/>, pak má přednost před zadáním <see cref="SizeRange"/>.
            /// Vyjadřuje prostor na vizuální ose ve směru Y, ve kterém bude zobrazeno obarvení tohoto segmentu.
            /// </summary>
            Int32Range HeightRange { get; }
            /// <summary>
            /// Rozmezí výšky na ose Y, které bude obarveno barvou <see cref="BackColor"/>.
            /// Povolené rozmezí = 0 až 1, což je i defaultní hodnota v případě null, značí celou výšku osy.
            /// Souběh zadání <see cref="SizeRange"/> a <see cref="HeightRange"/>:
            /// Pokud je zadáno <see cref="HeightRange"/>, pak má přednost před zadáním <see cref="SizeRange"/>.
            /// Vyjadřuje prostor na vizuální ose ve směru Y, ve kterém bude zobrazeno obarvení tohoto segmentu.
            /// </summary>
            DoubleRange SizeRange { get; }
            /// <summary>
            /// Text pro Tooltip v daném rozmezí, přidává se pod standardní text ToolTipu
            /// </summary>
            string ToolTip { get; }
            /// <summary>
            /// Aktuální rozmezí v pixelech na aktivní souřadnici osy (vodorovná osa = souřadnice X)
            /// </summary>
            Int32Range PixelRange { get; }
            /// <summary>
            /// Metoda připraví souřadnice do <see cref="ISegment.PixelRange"/> a vrátí true, pokud daný segment má být na aktuální ose zobrazen.
            /// </summary>
            /// <param name="axis"></param>
            /// <returns></returns>
            bool PrepareForAxis(BaseAxis<TTick, TSize, TValue> axis);
        }
        #endregion
        /// <summary>
        /// Vrací relativní pixelovou vzdálenost dané hodnoty od počátku osy.
        /// Pro hodnotu rovnou <see cref="Value"/>.Begin vrací tedy 0.
        /// </summary>
        /// <param name="tTick"></param>
        /// <returns></returns>
        public decimal? GetPixelForValue(TTick tTick)
        {
            TSize distance = this._ValueHelper.SubSize(tTick, this.Value.Begin);    // Vzdálenost TSize od hodnoty na počátku osy (Value.Begin) k danému bodu
            decimal? units = this.GetAxisUnits(distance);                           // Vzdálenost převedená na Units
            return this.GetPixelsFromUnits(units);                                  // Tatáž vzdálenost vyjádřená v pixelech = od počátku osy
        }
        #endregion
        #region Abstract layer: members, which descendant must override for actual data types
        /// <summary>
        /// Initial value for new axis
        /// </summary>
        protected abstract TValue InitialValue { get; }
        /// <summary>
        /// Returns a new instance of TValue for specified begin and end of interval.
        /// </summary>
        /// <param name="begin">Value of Begin interval</param>
        /// <param name="end">Value of End interval</param>
        /// <returns></returns>
        protected abstract TValue GetValue(TTick begin, TTick end);
        /// <summary>
        /// Returns a decimal number of units for specified interval.
        /// This is reverse method for GetAxisSize().
        /// For example: 
        /// on SizeAxis (in milimeters = Decimal, Decimal) returns (decimal)interval.
        /// on TimeAxis (in time = DateTime, TimeSpan) returns (decimal)interval.TotalSeconds.
        /// And so on...
        /// </summary>
        /// <param name="interval">Size of interval, which number of units must be returned</param>
        /// <returns></returns>
        protected abstract decimal? GetAxisUnits(TSize interval);
        /// <summary>
        /// Returns a TSize value, corresponding to specified units.
        /// This is reverse method for GetAxisUnits().
        /// For example: 
        /// on SizeAxis (in milimeters = Decimal, Decimal) returns (decimal)interval.
        /// on TimeAxis (in time = DateTime, TimeSpan) returns TimeSpan.FromSeconds((double)units).
        /// And so on...
        /// </summary>
        /// <param name="units">Number of units, from which must be returned an Size of interval</param>
        /// <returns></returns>
        protected abstract TSize GetAxisSize(decimal units);
        /// <summary>
        /// Returns a string representation of value of Tick, using string Format from ArrangementItem for this Tick.
        /// Typically return tick.ToString(format), for real TTick type.
        /// </summary>
        /// <param name="tick">Value of Tick</param>
        /// <param name="format">Format string on ArrangementItem of this Tick</param>
        /// <returns></returns>
        protected abstract string GetTickText(TTick tick, string format);
        /// <summary>
        /// Zaokrouhlí a vrátí danou hodnotu (value) do daného intervalu v daném směru.
        /// Například pro TimeAxis: pokud value je 15.2.2016 14:35:16.165; a interval je 00:15:00.000 a roundMode = Math,
        /// pak výsledek je 15.2.2016 14:30:00
        /// </summary>
        /// <param name="value">Hodnota (Tick) k zaokrouhlení</param>
        /// <param name="interval">Interval, do kterého bude Tick zaokrouhlen</param>
        /// <param name="roundMode">Režim zaokrouhlení</param>
        /// <returns></returns>
        protected abstract TTick RoundTickToInterval(TTick value, TSize interval, RoundMode roundMode);
        /// <summary>
        /// Axis class here declared items (ArrangementOne) for use in Axis.
        /// Each individual ArrangementOne contains specification for range of scale on axis, declare distance of axis ticks by tick types (pixel, small, standard, big...) 
        /// and contains format strings for ticks.
        /// Each ArrangementOne must contain several ArrangementItem, each Item for one Tick type.
        /// Many of ArrangementOne is stored in one ArrangementSet (this.Arrangement).
        /// In one time is active only one ArrangementOne (with few ArrangementItem for axis ticks). This one ArrangementOne is valid for small range of Scale.
        /// As the Scale is changed, then ArrangementSet select other ArrangementOne, appropriate for new Scale (containing other definition of its Items), 
        /// and this behavior accomodate Axis visual representation (ticks, labels) to changed Scale.
        /// When Scale is not changed, only change Begin (or End) of Value, then previous selected ArrangementOne is not changed.
        /// <para></para>
        /// InitAxisArrangement() method declare many of ArrangementOne, each with definiton for AxisTickType, and send this ArrangementOne to AddArrangementOne() method, for example:
        /// </summary>
        protected abstract void InitAxisArrangement();
        #endregion
    }
    #region class GBaseAxisTickPainter : Axis Tick Painter
    /// <summary>
    /// GBaseAxisTickPainter : Axis Tick Painter
    /// </summary>
    public class BaseAxisTickPainter : IDisposable
    {
        #region Konstrukce, property, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="verticalText"></param>
        public BaseAxisTickPainter(Rectangle bounds, AxisOrientation orientation, bool verticalText)
        {
            this.Bounds = bounds;
            this.Orientation = orientation;
            this.VerticalText = verticalText;

            this.TickFontT = new FontInfo() { FontType = FontSetType.DefaultFont, SizeRatio = 0.90f, Bold = true };
            this.TickFontS = new FontInfo() { FontType = FontSetType.DefaultFont, SizeRatio = 0.80f };
            this.StringFormat = new StringFormat(StringFormatFlags.NoClip);

            this.CalculateInternal();
        }
        /// <summary>
        /// Vypočte interní souřadnice podle Bounds a Orientation
        /// </summary>
        private void CalculateInternal()
        {
            switch (this.Orientation)
            {
                case AxisOrientation.Top:
                case AxisOrientation.Bottom:
                    this.AxisLength = this.Bounds.Width;
                    this.AxisWidth = this.Bounds.Height;
                    break;
                case AxisOrientation.LeftDown:
                case AxisOrientation.LeftUp:
                case AxisOrientation.RightDown:
                case AxisOrientation.RightUp:
                    this.AxisLength = this.Bounds.Height;
                    this.AxisWidth = this.Bounds.Width;
                    break;
                default:
                    this.AxisLength = this.Bounds.Width;
                    this.AxisWidth = this.Bounds.Height;
                    break;
            }

            int wd = 14;
            if (this.IsVertical && !this.VerticalText)
                wd = 42;
            int len = this.AxisWidth - wd;
            this.TickLengthInitial = CalculateLength(len, 5, 37);
            this.TickLengthTitle = CalculateLength(this.TickLengthInitial - 14, 5, 30);
            this.TickLengthSubTitle = CalculateLength(this.TickLengthTitle - 3, 4, 25);
            this.TickLengthSignificant = CalculateLength(this.TickLengthSubTitle - 2, 3, 22);
            this.TickLengthRegular = CalculateLength(this.TickLengthSignificant - 2, 2, 20);
        }
        /// <summary>
        /// Zarovná délku do daných mezí
        /// </summary>
        /// <param name="len"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private static int CalculateLength(int len, int min, int max)
        {
            if (len < min) return min;
            if (len > max) return max;
            return len;
        }
        /// <summary>
        /// Font pro psaní SubTitle
        /// </summary>
        public FontInfo TickFontT { get; private set; }
        /// <summary>
        /// Font pro psaní SubTitle a Initial
        /// </summary>
        public FontInfo TickFontS { get; private set; }
        /// <summary>
        /// Font pro psaní aktuálního ticku
        /// </summary>
        protected FontInfo TickFont { get; private set; }
        /// <summary>
        /// String Format pro text
        /// </summary>
        public StringFormat StringFormat { get; private set; }
        /// <summary>
        /// Rozměry Axis
        /// </summary>
        public Rectangle Bounds { get; private set; }
        /// <summary>
        /// Orientace
        /// </summary>
        public AxisOrientation Orientation { get; private set; }
        /// <summary>
        /// true = Paint text on vertical axis with vertical font;
        /// false (default) = Paint text in horizontal line on vertical axis
        /// </summary>
        public bool VerticalText { get; private set; }
        /// <summary>
        /// Orientace je vodorovná?
        /// </summary>
        public bool IsHorizontal { get { return (this.Orientation == AxisOrientation.Top || this.Orientation == AxisOrientation.Bottom); } }
        /// <summary>
        /// Orientace je svislá?
        /// </summary>
        public bool IsVertical { get { return (this.Orientation == AxisOrientation.LeftUp || this.Orientation == AxisOrientation.LeftDown || this.Orientation == AxisOrientation.RightUp || this.Orientation == AxisOrientation.RightDown); } }
        /// <summary>
        /// Orientace je vlevo?
        /// </summary>
        public bool IsOnLeft { get { return (this.Orientation == AxisOrientation.LeftUp || this.Orientation == AxisOrientation.LeftDown); } }
        /// <summary>
        /// Orientace je vpravo?
        /// </summary>
        public bool IsOnRight { get { return (this.Orientation == AxisOrientation.RightUp || this.Orientation == AxisOrientation.RightDown); } }
        /// <summary>
        /// Délka osy ve směru v němž osa běží (délka pravítka)
        /// </summary>
        public int AxisLength { get; private set; }
        /// <summary>
        /// Šířka osy, ve směru šířky (šířka pravítka)
        /// </summary>
        public int AxisWidth { get; private set; }
        /// <summary>
        /// Linka textu Initial, na které je posazen text (při orientaci Top je to souřadnice Y měřená od spodu osy)
        /// </summary>
        public int TickLengthInitial { get; private set; }
        /// <summary>
        /// Linka textu Title, na které je posazen text (při orientaci Top je to souřadnice Y měřená od spodu osy)
        /// </summary>
        public int TickLengthTitle { get; private set; }
        /// <summary>
        /// Linka textu SubTitle, na které je posazen text (při orientaci Top je to souřadnice Y měřená od spodu osy)
        /// </summary>
        public int TickLengthSubTitle { get; private set; }
        /// <summary>
        /// Velikost ticků Significant
        /// </summary>
        public int TickLengthSignificant { get; private set; }
        /// <summary>
        /// Velikost ticků Regular
        /// </summary>
        public int TickLengthRegular { get; private set; }
        /// <summary>
        /// Dispose
        /// </summary>
        void IDisposable.Dispose()
        {
            this.TickFont = null;             // Here is only another reference to object TickFontT or TickFontS

            if (this.StringFormat != null) this.StringFormat.Dispose();
            this.StringFormat = null;
        }
        #endregion
        #region Kreslení ticků
        /// <summary>
        /// Kreslí jeden daný tick na ose, jejíž základní charakteristiky jsou uložené v this
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="tick"></param>
        public void DrawTick(Graphics graphics, VisualTick tick)
        {
            // Line:
            Point point1, point2;
            Pen pen;
            if (this.CalculateLinePoints(tick, out point1, out point2, out pen))
                graphics.DrawLine(pen, point1, point2);

            // Text:
            Rectangle textBound;
            Brush brush;
            if (this.CalculateTextBounds(graphics, tick, point1, out textBound, out brush))
                graphics.DrawString(tick.Text, this.TickFont.Font, brush, textBound, this.StringFormat);
        }
        /// <summary>
        /// Calculate coordinates of point for tick line on current Bounds and Orientation, for specified Tick.
        /// Set value: point1 is near to text, point2 is near to axis edge.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="pen"></param>
        /// <returns></returns>
        protected bool CalculateLinePoints(VisualTick tick, out Point point1, out Point point2, out Pen pen)
        {
            Rectangle b = this.Bounds;
            bool bigTick = (tick.TickType == AxisTickType.BigLabel || tick.TickType == AxisTickType.StdLabel);
            float width = (bigTick ? 2f : 1f);
            int aw = (bigTick ? 1 : 0);
            int tickLength = this.GetLineLength(tick.TickType);
            int x1 = 0;
            int x2 = 0;
            int y1 = 0;
            int y2 = 0;
            switch (this.Orientation)
            {
                case AxisOrientation.Top:
                    x1 = b.X + tick.RelativePixel;
                    x2 = x1;
                    y2 = b.Bottom - 2;
                    y1 = y2 - tickLength;
                    y2 += aw;
                    break;
                case AxisOrientation.LeftUp:
                    x2 = b.Right - 2;
                    x1 = x2 - tickLength;
                    x2 += aw;
                    y1 = b.Bottom - 1 - tick.RelativePixel;
                    y2 = y1;
                    break;
                case AxisOrientation.LeftDown:
                    x2 = b.Right - 2;
                    x1 = x2 - tickLength;
                    x2 += aw;
                    y1 = b.Y + tick.RelativePixel;
                    y2 = y1;
                    break;
                case AxisOrientation.RightUp:
                    x2 = b.X + 1;
                    x1 = x2 + tickLength;
                    x1 += aw;
                    y1 = b.Bottom - 1 - tick.RelativePixel;
                    y2 = y1;
                    break;
                case AxisOrientation.RightDown:
                    x2 = b.X + 1;
                    x1 = x2 + tickLength;
                    x1 += aw;
                    y1 = b.Y + tick.RelativePixel;
                    y2 = y1;
                    break;
                case AxisOrientation.Bottom:
                    x1 = b.X + tick.RelativePixel;
                    x2 = x1;
                    y2 = b.Y + 1;
                    y1 = y2 + tickLength;
                    y1 += aw;
                    break;
            }
            point1 = new Point(x1, y1);
            point2 = new Point(x2, y2);
            pen = Skin.Pen((tick.TickType == AxisTickType.StdTick ? Skin.Axis.LineColorTickStandard : Skin.Axis.LineColorTickBig), width);
            return true;
        }
        /// <summary>
        /// Calculate coordinates of text for label for specified Tick.
        /// Value point1 is line point, near to text.
        /// </summary>
        /// <param name="graphics">Grafika, pomáhá umístit text</param>
        /// <param name="tick"></param>
        /// <param name="point1"></param>
        /// <param name="textBound"></param>
        /// <param name="brush"></param>
        /// <returns></returns>
        protected bool CalculateTextBounds(Graphics graphics, VisualTick tick, Point point1, out Rectangle textBound, out Brush brush)
        {
            textBound = Rectangle.Empty;
            brush = null;
            if (!(tick.TickType == AxisTickType.OuterLabel || tick.TickType == AxisTickType.BigLabel || tick.TickType == AxisTickType.StdLabel)) return false;
            if (String.IsNullOrEmpty(tick.Text)) return false;

            bool bigTick = (tick.TickType != AxisTickType.StdLabel);
            this.TickFont = (bigTick ? this.TickFontT : this.TickFontS);
            bool verticalText = this.IsVertical && this.VerticalText;
            this.StringFormat.FormatFlags = (verticalText ? StringFormatFlags.NoClip | StringFormatFlags.DirectionVertical : StringFormatFlags.NoClip);

            SizeF size = graphics.MeasureString(tick.Text, this.TickFont.Font, PointF.Empty, this.StringFormat);
            AxisTickAlignment alignment = tick.Alignment;
            int x = point1.X;
            int y = point1.Y;
            int x1 = 0;
            int y1 = 0;
            int tw = (int)(size.Width + 2f);
            int th = (int)(size.Height + 2f);
            switch (this.Orientation)
            {
                case AxisOrientation.Top:
                    x1 = x - GetAlignmentSize(alignment, tw);
                    y1 = y - th + 1;
                    break;
                case AxisOrientation.LeftUp:
                    x1 = x - tw + 1;
                    y1 = y - GetAlignmentSize(alignment, th);
                    break;
                case AxisOrientation.LeftDown:
                    x1 = x - tw + 1;
                    y1 = y - GetAlignmentSize(alignment, th);
                    break;
                case AxisOrientation.RightUp:
                    x1 = x + 1;
                    y1 = y - GetAlignmentSize(alignment, th);
                    break;
                case AxisOrientation.RightDown:
                    x1 = x + 1;
                    y1 = y - GetAlignmentSize(alignment, th);
                    break;
                case AxisOrientation.Bottom:
                    x1 = x - GetAlignmentSize(alignment, tw);
                    y1 = y - 1;
                    break;
            }

            textBound = new Rectangle(new Point(x1, y1), Size.Ceiling(size));
            if (textBound.X < this.Bounds.X) textBound.X = this.Bounds.X;
            if (textBound.Y < this.Bounds.Y) textBound.Y = this.Bounds.Y;

            brush = Skin.Brush(bigTick ? Skin.Axis.TextColorLabelBig : Skin.Axis.TextColorLabelStandard);

            return true;
        }
        /// <summary>
        /// Returns size alignment: for Begin return 0, for Center return size/2, for End return size.
        /// </summary>
        /// <param name="alignment"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        protected int GetAlignmentSize(AxisTickAlignment alignment, float size)
        {
            switch (alignment)
            {
                case AxisTickAlignment.Begin: return 0;
                case AxisTickAlignment.Center: return (int)(size / 2f);
                case AxisTickAlignment.End: return (int)size;
            }
            return 0;
        }
        /// <summary>
        /// Returns a tick length for specified tick type
        /// </summary>
        /// <param name="tickType"></param>
        /// <returns></returns>
        protected int GetLineLength(AxisTickType tickType)
        {
            switch (tickType)
            {
                case AxisTickType.None: return 0;
                case AxisTickType.Pixel: return 1;
                case AxisTickType.StdTick: return this.TickLengthRegular;
                case AxisTickType.BigTick: return this.TickLengthSignificant;
                case AxisTickType.StdLabel: return this.TickLengthSubTitle;
                case AxisTickType.BigLabel: return this.TickLengthTitle;
                case AxisTickType.OuterLabel: return this.TickLengthInitial;
            }
            return 0;
        }
        #endregion
    }
    #endregion
    #region Common Axis members: class BaseTick, VisualTick; enums AxisTickType, AxisTickAlignment, AxisOrientation, AxisSynchronizeMode
    /// <summary>
    /// One real tick on any Axis (generic value)
    /// </summary>
    public class BaseTick<T> : VisualTick
    {
        /// <summary>
        /// Create new instance of Tick
        /// </summary>
        /// <param name="tickType">Type of this tick</param>
        /// <param name="value">Logical value of tick (DateTime for TimeAxis, Decimal for SizeAxis...)</param>
        /// <param name="relativePixel">Cordinate of tick in pixels, relative to the begin of axis</param>
        /// <param name="sizeRatio">Standard ratio length of tick between 0 and 1. Title has length = 1.00, SubTitle = 0.90, SignificantTick = 0.75, RegularTick = 0.60.</param>
        /// <param name="text">Displayed value of tick</param>
        /// <param name="alignment">Alignement of label</param>
        public BaseTick(AxisTickType tickType, T value, int relativePixel, decimal sizeRatio, string text, AxisTickAlignment alignment)
            : base(tickType, relativePixel, sizeRatio, text, alignment)
        {
            this.Value = value;
        }
        /// <summary>
        /// Logical value of tick (DateTime, Decimal)
        /// </summary>
        public T Value { get; private set; }
    }
    /// <summary>
    /// Data popisující jeden konkrétní tick na ose, bez hodnoty Ticku, pouze jeho typ, text, relativn pixel)
    /// </summary>
    public class VisualTick
    {
        /// <summary>
        /// Vytvoří instanci Ticku
        /// </summary>
        /// <param name="tickType">Type of this tick</param>
        /// <param name="relativePixel">Cordinate of tick in pixels, relative to the begin of axis. Value 0 = at begin of Axis (at point Axis.RelativeVisualBounds.Left, .Top, .Bottom)</param>
        /// <param name="sizeRatio">Standard ratio length of tick between 0 and 1. Title has length = 1.00, SubTitle = 0.90, SignificantTick = 0.75, RegularTick = 0.60.</param>
        /// <param name="text">Displayed value of tick</param>
        /// <param name="alignment">Alignement of label</param>
        public VisualTick(AxisTickType tickType, int relativePixel, decimal sizeRatio, string text, AxisTickAlignment alignment)
        {
            this.TickType = tickType;
            this.RelativePixel = relativePixel;
            this.SizeRatio = sizeRatio;
            this.Text = text;
            this.Alignment = alignment;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text + "; AtPixel=" + this.RelativePixel.ToString() + "; " + this.TickType.ToString();
        }
        /// <summary>
        /// Type of this tick
        /// </summary>
        public AxisTickType TickType { get; private set; }
        /// <summary>
        /// Cordinate of tick in pixels, relative to the begin of axis.
        /// Value 0 = at begin of Axis (at point Axis.RelativeVisualBounds.Left, .Top, .Bottom)
        /// </summary>
        public int RelativePixel { get; private set; }
        /// <summary>
        /// Standard ratio length of tick between 0 and 1. 
        /// Title has length = 1.00, SubTitle = 0.90, SignificantTick = 0.75, RegularTick = 0.60.
        /// </summary>
        public decimal SizeRatio { get; private set; }
        /// <summary>
        /// Displayed value of tick
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Alignement of label
        /// </summary>
        public AxisTickAlignment Alignment { get; private set; }
    }
    /// <summary>
    /// Typ ticku, tick je čárka (nebo místo) na ose, symbolizující konkrétní hodnotu.
    /// Podobně jako na plastovém pravítku jsou graficky odlišeny ticky pro 1mm, pro 5mm, pro 1cm, pro 10cm.
    /// </summary>
    public enum AxisTickType : int
    {
        /// <summary>
        /// Neurčeno. Symbolizuje volný pohyb po ose.
        /// </summary>
        None = 0,
        /// <summary>
        /// Pixel. Nezobrazuje se, slouží k zaokrouhlování hodnoty na konkrétní pixely.
        /// </summary>
        Pixel = 1,
        /// <summary>
        /// Standardní nejmenší zobrazovaný tick, bez popisku, obdoba 1mm čárky na pravítku
        /// </summary>
        StdTick = 2,
        /// <summary>
        /// Větší zobrazovaný tick, bez popisku, obdoba 5mm čárky na pravítku
        /// </summary>
        BigTick = 3,
        /// <summary>
        /// Standardní tick s popiskem, obdoba 1cm čárky na pravítku
        /// </summary>
        StdLabel = 4,
        /// <summary>
        /// Velký tick s popiskem, obdoba 10cm čárky na pravítku
        /// </summary>
        BigLabel = 5,
        /// <summary>
        /// Okraj osy
        /// </summary>
        OuterLabel = 6
    }
    /// <summary>
    /// Typ zarovnání ticku na ose
    /// </summary>
    public enum AxisTickAlignment
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,
        /// <summary>
        /// K počátku
        /// </summary>
        Begin,
        /// <summary>
        /// Na střed
        /// </summary>
        Center,
        /// <summary>
        /// Ke konci
        /// </summary>
        End
    }
    /// <summary>
    /// Orientace a umístění osy
    /// </summary>
    public enum AxisOrientation
    {
        /// <summary>
        /// Osa je vodorovná, na straně Top oblasti, kde jsou data. 
        /// Směr osy je zleva (od menších hodnot) doprava (k větším hodnotám).
        /// Ticky jsou zobrazeny na dolním okraji osy.
        /// </summary>
        Top = 0,
        /// <summary>
        /// Osa je svislá, nahoru, na straně Left oblasti, kde jsou data. 
        /// Směr osy je zdola (od menších hodnot) nahoru (k větším hodnotám).
        /// Ticky jsou zobrazeny na pravém okraji osy.
        /// </summary>
        LeftUp,
        /// <summary>
        /// Osa je svislá, dolů, na straně Left oblasti, kde jsou data. 
        /// Směr osy je zeshora (od menších hodnot) dolů (k větším hodnotám).
        /// Ticky jsou zobrazeny na pravém okraji osy.
        /// </summary>
        LeftDown,
        /// <summary>
        /// Osa je svislá, nahoru, na straně Right oblasti, kde jsou data. 
        /// Směr osy je zdola (od menších hodnot) nahoru (k větším hodnotám).
        /// Ticky jsou zobrazeny na levém okraji osy.
        /// </summary>
        RightUp,
        /// <summary>
        /// Osa je svislá, dolů, na straně Right oblasti, kde jsou data. 
        /// Směr osy je zeshora (od menších hodnot) dolů (k větším hodnotám).
        /// Ticky jsou zobrazeny na levém okraji osy.
        /// </summary>
        RightDown,
        /// <summary>
        /// Osa je vodorovná, na straně Bottom oblasti, kde jsou data. 
        /// Směr osy je zleva (od menších hodnot) doprava (k větším hodnotám).
        /// Ticky jsou zobrazeny na horním okraji osy.
        /// </summary>
        Bottom
    }
    /// <summary>
    /// 
    /// </summary>
    public enum AxisSynchronizeMode
    {
        /// <summary>
        /// No synchronize
        /// </summary>
        None = 0,
        /// <summary>
        /// Synchronize Scale, no Range
        /// </summary>
        Scale,
        /// <summary>
        /// Synchronize Range
        /// </summary>
        Range
    }
    /// <summary>
    /// Stav interaktivity na ose
    /// </summary>
    public enum AxisInteractiveState
    {
        /// <summary>
        /// Nic
        /// </summary>
        None,
        /// <summary>
        /// Myš je nad osou, osa se nijak nepohybuje
        /// </summary>
        MouseOver,
        /// <summary>
        /// Myš táhne osu na novou pozici (Shift)
        /// </summary>
        DragMove,
        /// <summary>
        /// Myš mění měřítko na novou hodnotu (Zoomování)
        /// </summary>
        DragZoom
    }
    #endregion
}
