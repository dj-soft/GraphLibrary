using Asol.Tools.WorkScheduler.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// TrackBar : dovoluje nastavit hodnotu pomocí jezdce nebo jiného gripu, umístěného na ploše controlu
    /// </summary>
    public class GTrackBar : InteractiveObject
    {
        #region Konstruktor a privátní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTrackBar() : base()
        {
            this._Value = 0m;
            this._ValueTotal = new DecimalRange(0m, 1m);
            this._InactiveFrame = new System.Windows.Forms.Padding(5);
            this._Orientation = System.Windows.Forms.Orientation.Horizontal;
            this._TrackPointerVisualSize = new Size(8, 15);
            this.Is.MouseMoveOver = true;
            this.Is.MouseDragMove = true;
            this.BackColor = Skin.TrackBar.BackColorTrack;
        }
        private Decimal _Value;
        private DecimalRange _ValueTotal;
        private System.Windows.Forms.Orientation _Orientation;
        private Int32? _TickCount;
        private Size _TrackPointerVisualSize;
        private System.Windows.Forms.Padding _InactiveFrame;
        #endregion
        #region Data : Value, ValueTotal
        /// <summary>
        /// Aktuálně zobrazená hodnota.
        /// </summary>
        public Decimal Value
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.All, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Obsahuje relativní hodnotu trackbaru v rozmezí 0 až 1
        /// </summary>
        public Decimal? ValueRelative
        {
            get { return (this._ValueTotal != null ? this._ValueTotal.GetRelativePositionAtValue(this._Value) : (Decimal?)null); }
        }
        /// <summary>
        /// Aktuálně zobrazená hodnota. 
        /// Silent = vložení hodnoty do této property nespustí událost ValueChanged.
        /// </summary>
        protected Decimal ValueSilent
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.SilentValueActions, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Aktuálně zobrazená hodnota.
        /// Drag = vložení hodnoty do této property nevede k přepočtu vnitřních souřadnic = pozice thumbu, protože ta je právě zdrojem akce.
        /// </summary>
        protected Decimal ValueDrag
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.DragValueActions, EventSourceType.ValueChanging | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Hodnota <see cref="Value"/>, která byla v objektu na počátku akce DragMove. Mimo proces DragMove je null.
        /// </summary>
        protected Decimal? ValueDragOriginal { get; set; }
        /// <summary>
        /// Celkový rozsah pro nastavitelnou hodnotu.
        /// Hodnota <see cref="Value"/> může nabývat obou mezí, jak dolní tak horní, včetně.
        /// Výchozí rozsah je 0 až 1.
        /// </summary>
        public DecimalRange ValueTotal
        {
            get { return this._ValueTotal; }
            set { this.SetValueTotal(value, ProcessAction.All, EventSourceType.ValueRangeChange | EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Uloží danou hodnotu do this._Value.
        /// Provede požadované akce (ValueAlign, ScrollDataValidate, InnerBoundsReset, OnValueChanged).
        /// </summary>
        /// <param name="value"></param>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
        protected void SetValue(Decimal value, ProcessAction actions, EventSourceType eventSource)
        {
            Decimal oldValue = this._Value;
            Decimal newValue = value;
            if (IsAction(actions, ProcessAction.RecalcValue))
                newValue = this.ValueAlign(newValue);

            if (newValue == oldValue) return;    // No change = no reactions.

            this._Value = newValue;

            if (IsAction(actions, ProcessAction.PrepareInnerItems))
                this.ChildItemsReset();
            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallValueChanged(oldValue, newValue, eventSource);
            if (IsAction(actions, ProcessAction.CallChangingEvents))
                this.CallValueChanging(oldValue, newValue, eventSource);
            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Uloží danou hodnotu do this._ValueTotal.
        /// Provede požadované akce (ValueAlign, ScrollDataValidate, InnerBoundsReset, OnValueChanged).
        /// </summary>
        /// <param name="valueTotal"></param>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
        protected void SetValueTotal(DecimalRange valueTotal, ProcessAction actions, EventSourceType eventSource)
        {
            if (valueTotal == null) return;
            DecimalRange oldValueTotal = this._ValueTotal;
            DecimalRange newValueTotal = valueTotal;
            if (oldValueTotal != null && newValueTotal == oldValueTotal) return;    // No change = no reactions.

            this._ValueTotal = newValueTotal;

            if (IsAction(actions, ProcessAction.RecalcValue))
                this.SetValue(this._Value, LeaveOnlyActions(actions, ProcessAction.RecalcValue, ProcessAction.PrepareInnerItems, ProcessAction.CallChangedEvents), eventSource);
            if (IsAction(actions, ProcessAction.PrepareInnerItems))
                this.ChildItemsReset();
            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallValueTotalChanged(oldValueTotal, newValueTotal, eventSource);
            if (IsAction(actions, ProcessAction.CallDraw))
                this.CallDrawRequest(eventSource);
        }
        /// <summary>
        /// Zarovná hodnotu do aktuálního rozmezí
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected decimal ValueAlign(decimal value)
        {
            return this._ValueTotal.Align(value);
        }
        #endregion
        #region Public properties pro řízení vzhledu
        /// <summary>
        /// Orientace
        /// </summary>
        protected System.Windows.Forms.Orientation Orientation
        {
            get { return this._Orientation; }
            set { this._Orientation = value; this.Repaint(); }
        }
        /// <summary>
        /// Počet kreslených ticků.
        /// Přesněji = počet úseků TrackBaru, oddělených Ticky.
        /// Pokud je tedy <see cref="TickCount"/> == 2, pak je vykreslen počáteční a koncový bod TrackBaru, a mezi nimi jeden Tick, oddělující 2 úseky.
        /// Výchozí rozsah je null = nekreslí se.
        /// </summary>
        public Int32? TickCount
        {
            get { return this._TickCount; }
            set { this._TickCount = value; this.Repaint(); }
        }
        /// <summary>
        /// Vizuální velikost prvku TrackPointer, nezávislá na orientaci.
        /// Odpovídá fyzické velikosti při orientaci Horizontal.
        /// </summary>
        public Size TrackPointerVisualSize
        {
            get { return this._TrackPointerVisualSize; }
            set { this._TrackPointerVisualSize = value.Max(8, 8); this.Repaint(); }
        }
        /// <summary>
        /// Vnitřní neaktivní okraj (prostor mezi Bounds a prostorem, v němž je aktivní TrackBar)
        /// </summary>
        public System.Windows.Forms.Padding InactiveFrame
        {
            get { return this._InactiveFrame; }
            set { this._InactiveFrame = value; this.Repaint(); }
        }

        #endregion
        #region Volání událostí z this (ValueChanging, ValueChanged, ValueTotalChanged, DrawRequest)
        /// <summary>
        /// Vyvolá metodu OnValueChanging() a event ValueChanging
        /// </summary>
        protected void CallValueChanging(Decimal oldValue, Decimal newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<Decimal> args = new GPropertyChangeArgs<Decimal>(oldValue, newValue, eventSource);
            this.OnValueChanging(args);
            if (!this.IsSuppressedEvent && this.ValueChanging != null)
                this.ValueChanging(this, args);
        }
        /// <summary>
        /// Háček volaný v průběhu interaktivní změny hodnoty <see cref="Value"/>
        /// </summary>
        protected virtual void OnValueChanging(GPropertyChangeArgs<Decimal> args) { }
        /// <summary>
        /// Event volaný v průběhu interaktivní změny hodnoty <see cref="Value"/>
        /// </summary>
        public event GPropertyChangedHandler<Decimal> ValueChanging;

        /// <summary>
        /// Vyvolá metodu OnValueChanged() a event ValueChanged
        /// </summary>
        protected void CallValueChanged(Decimal oldValue, Decimal newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<Decimal> args = new GPropertyChangeArgs<Decimal>(oldValue, newValue, eventSource);
            this.OnValueChanged(args);
            if (!this.IsSuppressedEvent && this.ValueChanged != null)
                this.ValueChanged(this, args);
        }
        /// <summary>
        /// Háček volaný po skončení změny hodnoty <see cref="Value"/>
        /// </summary>
        protected virtual void OnValueChanged(GPropertyChangeArgs<Decimal> args) { }
        /// <summary>
        /// Event volaný po skončení změny hodnoty <see cref="Value"/>
        /// </summary>
        public event GPropertyChangedHandler<Decimal> ValueChanged;

        /// <summary>
        /// Vyvolá metodu OnValueTotalChanged() a event ValueTotalChanged
        /// </summary>
        protected void CallValueTotalChanged(DecimalRange oldValue, DecimalRange newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<DecimalRange> args = new GPropertyChangeArgs<DecimalRange>(oldValue, newValue, eventSource);
            this.OnValueTotalChanged(args);
            if (!this.IsSuppressedEvent && this.ValueTotalChanged != null)
                this.ValueTotalChanged(this, args);
        }
        /// <summary>
        /// Háček volaný po skončení změny hodnoty <see cref="ValueTotal"/>
        /// </summary>
        protected virtual void OnValueTotalChanged(GPropertyChangeArgs<DecimalRange> args) { }
        /// <summary>
        /// Event on this.Value changes
        /// </summary>
        public event GPropertyChangedHandler<DecimalRange> ValueTotalChanged;
        
        /// <summary>
        /// Call method OnUserDraw() and event UserDraw
        /// </summary>
        protected void CallUserDraw(GUserDrawArgs e)
        {
            this.OnUserDraw(e);
            if (!this.IsSuppressedEvent && this.UserDraw != null)
                this.UserDraw(this, e);
        }
        /// <summary>
        /// Occured after change value in this.Visual*Bounds
        /// </summary>
        protected virtual void OnUserDraw(GUserDrawArgs e) { }
        /// <summary>
        /// Event on this.DrawRequest occured
        /// </summary>
        public event GUserDrawHandler UserDraw;

        #endregion
        #region ChildItems
        /// <summary>
        /// Child items
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this.ChildItemsCheck(); return this._Childs; } }
        /// <summary>
        /// Resetuje Child items
        /// </summary>
        protected void ChildItemsReset()
        {
            this._Childs = null;
        }
        /// <summary>
        /// Zajistí platnost Child items
        /// </summary>
        protected void ChildItemsCheck()
        {
            if (this._Childs != null) return;
            this._Childs = new IInteractiveItem[0];
        }
        /// <summary>
        /// Child items
        /// </summary>
        protected IInteractiveItem[] _Childs;
        #endregion
        #region Interaktivita
        /// <summary>
        /// Řeší interaktivitu
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            PartType partType = this.GetPartType(e);
            switch (e.ChangeState)
            {
                case GInteractiveChangeState.MouseOver:
                    this.MouseOver(partType, e);
                    break;
                case GInteractiveChangeState.LeftDown:
                    this.LeftDown(partType, e);
                    break;
                case GInteractiveChangeState.LeftDragMoveBegin:
                    this.LeftDragBegin(partType, e);
                    break;
                case GInteractiveChangeState.LeftDragMoveStep:
                    this.LeftDragMove(partType, e);
                    break;
                case GInteractiveChangeState.LeftDragMoveDone:
                    this.LeftDragDrop(partType, e);
                    break;
                case GInteractiveChangeState.MouseLeave:
                    this.MouseLeave(partType, e);
                    break;
            }
        }
        /// <summary>
        /// Provede obsluhu MouseOver
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void MouseOver(PartType partType, GInteractiveChangeStateArgs e)
        {
            this.MouseOverPoint = e.MouseAbsolutePoint;
            if (partType != this.LastMouseOverPart)
                this.LastMouseOverPart = partType;
            this.Repaint();
        }
        /// <summary>
        /// Provede obsluhu MouseLeave
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void MouseLeave(PartType partType, GInteractiveChangeStateArgs e)
        {
            this.LastMouseOverPart = PartType.None;
            this.MouseDragOffset = null;
            this.MouseOverPoint = null;
            this.Repaint();
        }
        /// <summary>
        /// Provede obsluhu LeftDown
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void LeftDown(PartType partType, GInteractiveChangeStateArgs e)
        {
            switch (partType)
            {
                case PartType.NonActive:
                case PartType.Area:
                    // Kliknuto do aktivní plochy, ale ne do oblasti TrackPointeru:
                    //  => okamžitě přemístíme TrackPointeru na daný bod, a budeme očekávat Drag and Move:
                    this.ValueDragOriginal = this.Value;
                    this.ValueDrag = this.GetTrackValue(e);
                    this.MouseDragOffset = new Point(0, 0);
                    this.LastMouseOverPart = partType;
                    this.Repaint();
                    break;
                case PartType.Pointer:
                    // Kliknuto do prostoru TrackPointeru:
                    //  => TrackPointer nikam nepřemísťujeme, určíme offset pozice myši od TrackPointeru, a počkáme jestli uživatel sám provede Drag and Move:
                    this.ValueDragOriginal = this.Value;
                    this.MouseDragOffset = e.MouseRelativePoint.Value.Sub(this.TrackPoint);
                    this.Repaint();
                    break;
            }
        }
        /// <summary>
        /// Provede obsluhu MouseDrag Begin
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void LeftDragBegin(PartType partType, GInteractiveChangeStateArgs e)
        {
            this.MouseOverPoint = null;
            this.Repaint();
        }
        /// <summary>
        /// Provede obsluhu MouseDrag Move
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void LeftDragMove(PartType partType, GInteractiveChangeStateArgs e)
        {
            if (!this.MouseDragOffset.HasValue || !e.MouseRelativePoint.HasValue) return;

            Point dragPoint = e.MouseRelativePoint.Value.Sub(this.MouseDragOffset.Value);
            this.ValueDrag = this.GetValueForPoint(dragPoint);
            this.Repaint();
        }
        /// <summary>
        /// Provede obsluhu MouseDrag Drop
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void LeftDragDrop(PartType partType, GInteractiveChangeStateArgs e)
        {
            this.Value = this.ValueDrag;
            this.ValueDragOriginal = null;
            this.MouseDragOffset = null;
            this.Repaint();
        }
        /// <summary>
        /// Metoda vrátí oblast, ve které se pohybuje myš
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual PartType GetPartType(GInteractiveChangeStateArgs e)
        {
            Point? mouseRelativePoint = e.MouseRelativePoint;              // Souřadnice myši relativně k this, tj. hodnota 0/0 znamená levý horní roh TrackBaru
            if (!mouseRelativePoint.HasValue) return PartType.None;
            Point mouse = mouseRelativePoint.Value;

            Rectangle trackPointerBounds = this.TrackPointerActiveBounds;
            if (trackPointerBounds.Contains(mouse)) return PartType.Pointer;

            Rectangle activeBounds = this.ActiveBounds;
            if (activeBounds.Contains(mouse)) return PartType.Area;

            return PartType.NonActive;
        }
        /// <summary>
        /// Metoda vrátí bod nejbližší k bodu (e.MouseRelativePoint), který leží na dráze posuvníku <see cref="TrackLine"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual decimal GetTrackValue(GInteractiveChangeStateArgs e)
        {
            Rectangle trackLine = this.TrackLine;
            Point trackPoint = (e.MouseRelativePoint.HasValue ? e.MouseRelativePoint.Value.FitInto(trackLine) : trackLine.Location);
            decimal value = this.GetValueForPoint(trackPoint, trackLine);
            return value;
        }
        /// <summary>
        /// Bod myši při MouseOver
        /// </summary>
        protected Point? MouseOverPoint { get; set; }
        /// <summary>
        /// Offset myši proti Pointeru při začátku Drag and Drop = (MousePoint - TrackPoint)
        /// </summary>
        protected Point? MouseDragOffset { get; set; }
        /// <summary>
        /// Bod přetahování
        /// </summary>
        protected Point? MouseDragPoint { get; set; }
        /// <summary>
        /// Část prostoru, nad nímž byla myš při posledním MouseOver
        /// </summary>
        protected PartType LastMouseOverPart { get; set; }
        /// <summary>
        /// Typ pozice v TrackBaru
        /// </summary>
        public enum PartType
        {
            /// <summary>
            /// Mimo
            /// </summary>
            None = 0,
            /// <summary>
            /// V neaktivní oblasti
            /// </summary>
            NonActive,
            /// <summary>
            /// V aktivní oblasti mimo Pointer
            /// </summary>
            Area,
            /// <summary>
            /// V pointeru
            /// </summary>
            Pointer
        }
        #endregion
        #region Výpočty souřadnic jednotlivých částí TrackBaru, relativně k TrackBar.Bounds, podle hodnoty Value
        /// <summary>
        /// Vizuální velikost prvku TrackPointer, reálná při aktuální orientaci
        /// </summary>
        public Size TrackPointerCurrentVisualSize
        {
            get
            {
                Size size = this._TrackPointerVisualSize;
                return (this.Orientation == System.Windows.Forms.Orientation.Horizontal ? size : new Size(size.Height, size.Width));
            }
        }
        /// <summary>
        /// Vizuální souřadnice TrackPointu
        /// </summary>
        protected Rectangle TrackPointerVisualBounds
        {
            get
            {
                Point point = this.TrackPoint;
                Size size = this.TrackPointerCurrentVisualSize;
                return size.CreateRectangleFromCenter(point);
            }
        }
        /// <summary>
        /// Interaktivní souřadnice TrackPointu = jsou větší o 2px
        /// </summary>
        protected Rectangle TrackPointerActiveBounds
        {
            get
            {
                Rectangle bounds = this.TrackPointerVisualBounds;
                return bounds.Enlarge(2);
            }
        }
        /// <summary>
        /// Přesný bod středu TrackPointu, odpovídající aktuální hodnotě TrackBaru.
        /// Souřadnice je relativní = v souřadnicích TrackBar.Bounds.
        /// </summary>
        protected Point TrackPoint { get { return this.GetTrackPoint(this.ValueRelative); } }
        /// <summary>
        /// Metoda vrátí souřadnici bodu (relativní = v souřadnicích TrackBar.Bounds) pro danou relativní hodnotu 0 až 1.
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        protected Point GetTrackPoint(Decimal? ratio)
        {
            return this.GetTrackPoint(ratio, this.TrackLine);
        }
        /// <summary>
        /// Metoda vrátí souřadnici bodu (relativní = v souřadnicích TrackBar.Bounds) pro danou relativní hodnotu 0 až 1.
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="trackLine">Souřadnice pohybu TrackPointu</param>
        /// <returns></returns>
        protected Point GetTrackPoint(Decimal? ratio, Rectangle trackLine)
        {
            decimal r = (ratio.HasValue ? (ratio.Value < 0m ? 0m : (ratio.Value > 1m ? 1m : ratio.Value)) : 0m);

            switch (this.Orientation)
            {
                case System.Windows.Forms.Orientation.Horizontal:
                    // Vodorovný trackbar má větší hodnoty ve směru doprava, tam kde WinForm má souřadnici X větší:
                    decimal w = trackLine.Width;
                    int dx = (int)(Math.Round(r * w, 0));
                    int x = trackLine.X + dx;
                    return new Point(x, trackLine.Y);
                case System.Windows.Forms.Orientation.Vertical:
                    // Svislý trackbar má větší hodnoty ve směru nahoru, tam kde WinForm má souřadnici Y menší:
                    decimal h = trackLine.Height;
                    int dy = (int)(Math.Round(r * h, 0));
                    int y = trackLine.Bottom - dy;
                    return new Point(trackLine.X, y);
            }

            return trackLine.Center();
        }
        /// <summary>
        /// Obsahuje souřadnice prostoru, v němž se může pohybovat aktivní bod trackbaru <see cref="TrackPoint"/>, relativní = v souřadnicích TrackBar.Bounds.
        /// U horizontálního TrackBaru je to vodorovná čára s Height = 0; u vertikálního je to svislá čára s Width = 0.
        /// </summary>
        protected Rectangle TrackLine
        {
            get
            {
                int trackSize = this.TrackPointerVisualSize.Width; // Size.Width se používá jako šířka TrackPointeru i kdyby byl na výšku (pak se použije na ose Y = jako fyzická výška)
                int s1 = trackSize / 2;                            // Část TrackPointeru před jeho ideální polovinou
                int s2 = trackSize - s1;                           // Část TrackPointeru za jeho ideální polovinou

                Rectangle activeBounds = this.ActiveBounds;
                Point center = activeBounds.Center();

                switch (this.Orientation)
                {
                    case System.Windows.Forms.Orientation.Horizontal:
                        int w = activeBounds.Width - 2 - trackSize;
                        int x0 = activeBounds.X + 1 + s1;
                        return new Rectangle(x0, center.Y, w, 0);
                    case System.Windows.Forms.Orientation.Vertical:
                        int h = activeBounds.Height - 2 - trackSize;
                        int y0 = activeBounds.Y + 1 + s2;
                        return new Rectangle(center.X, y0, 0, h);
                }

                return new Rectangle(center.X, center.Y, 0, 0);
            }
        }
        /// <summary>
        /// Relativní souřadnice vnitřního aktivního prostoru, relativní = vzhledem k TrackBaru.
        /// Pokud tedy TrackBar má souřadnice { 100, 20, 200, 40 } a má <see cref="GTrackBar.InactiveFrame"/> = 4, 
        /// pak <see cref="ActiveBounds"/> = { 104, 24, 192, 32 }.
        /// </summary>
        protected Rectangle ActiveBounds
        {
            get
            {
                Rectangle bounds = this.Bounds;
                Rectangle innerBounds = new Rectangle(0, 0, bounds.Width, bounds.Height);
                return innerBounds.Sub(this.InactiveFrame);
            }
        }
        #endregion
        #region Kreslení
        /// <summary>
        /// Vykreslí TrackBar
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            base.Draw(e, absoluteBounds, absoluteVisibleBounds, drawMode);               // Vykreslí BackColor
            e.GraphicsClipWith(absoluteVisibleBounds);
            this.DrawTrackBar(e, absoluteBounds, absoluteVisibleBounds, drawMode);
        }
        /// <summary>
        /// Vykreslení
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawTrackBar(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            Point absoluteOrigin = absoluteBounds.Location;

            Rectangle trackBounds = this.ActiveBounds.Add(absoluteOrigin);
            GPainter.DrawTrackTicks(e.Graphics, trackBounds, System.Windows.Forms.Orientation.Horizontal, this.TickCount);

            Rectangle pointerBounds = this.TrackPointerVisualBounds.Add(absoluteOrigin);
            Point trackPoint = this.TrackPoint.Add(absoluteOrigin);
            Size trackSize = this.TrackPointerVisualSize;
            GPainter.DrawTrackPointer(e.Graphics, trackPoint, trackSize, this.InteractiveState, TrackPointerType.OneSide, RectangleSide.None);

            if (this.MouseOverPoint.HasValue)
            {
                Color pointColor = (this.IsInInteractiveState(GInteractiveState.LeftDown, GInteractiveState.LeftDrag) ? Color.BlueViolet : Color.DimGray);
                using (GPainter.GraphicsUseSmooth(e.Graphics))
                {
                    e.Graphics.FillEllipse(Skin.Brush(pointColor), this.MouseOverPoint.Value.CreateRectangleFromCenter(8));
                }
            }

        }

        #endregion
        #region Práce s hodnotou trackbaru
        /// <summary>
        /// Vrací hodnotou pro daný bod a aktuální TrackLine
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected virtual Decimal GetValueForPoint(Point point)
        {
            return this.GetValueForPoint(point, this.TrackLine);
        }
        /// <summary>
        /// Vrací hodnotou pro daný bod a danou TrackLine
        /// </summary>
        /// <param name="point"></param>
        /// <param name="trackLine"></param>
        /// <returns></returns>
        protected virtual Decimal GetValueForPoint(Point point, Rectangle trackLine)
        {
            Decimal? ratio = ((this.Orientation == System.Windows.Forms.Orientation.Horizontal) ?
                DecimalRange.CreateFromBeginSize(trackLine.X, trackLine.Width).GetRelativePositionAtValue(point.X) :
                DecimalRange.CreateFromBeginSize(trackLine.Bottom, trackLine.Height).GetRelativePositionAtValue(point.Y));
            decimal r = (ratio.HasValue ? (ratio.Value < 0m ? 0m : (ratio.Value > 1m ? 1m : ratio.Value)) : 0m);
            return this._ValueTotal.GetValueAtRelativePosition(r);
        }
        /// <summary>
        /// Vrací bod pro danou hodnotu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual Point GetPointForValue(Decimal value)
        {
            return Point.Empty;
        }
        #endregion
    }
}
