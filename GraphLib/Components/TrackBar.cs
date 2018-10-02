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
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTrackBar() : base()
        {
            this._Value = 0m;
            this._ValueTotal = new DecimalRange(0m, 1m);
            this._Visualiser = new LinearTrackBar(System.Windows.Forms.Orientation.Horizontal);
            this._VisualiserType = TrackBarVisualiserType.LinearHorizontal;
            this.Is.MouseMoveOver = true;
            this.BackColor = Skin.TrackBar.BackColorTrack;
        }
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
        private Decimal _Value;
        private DecimalRange _ValueTotal;
        private Int32? _TickCount;
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
            this._Childs = this.Visualiser.ChildItems;
            // qqq;
        }
        /// <summary>
        /// Child items
        /// </summary>
        protected IInteractiveItem[] _Childs;
        #endregion
        #region Interaktivita a Draw
        /// <summary>
        /// Po změně interaktivního stavu
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);
            this.Visualiser.AfterStateChanged(e);
        }
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
            this.Visualiser.Draw(e, absoluteBounds, absoluteVisibleBounds, drawMode);    // Vykreslí konkrétní vizualizaci
        }
        #endregion
        #region Current Visualiser
        /// <summary>
        /// Typ vizualizeru.
        /// Nemá význam setovat hodnotu <see cref="TrackBarVisualiserType.Custom"/>, protože ta neurčuje konkrétní vizualizer 
        /// (taková hodnota bude ignorována).
        /// </summary>
        public TrackBarVisualiserType VisualiserType
        {
            get { return this._VisualiserType; }
            set
            {
                switch (value)
                {
                    case TrackBarVisualiserType.LinearHorizontal:
                        this.Visualiser = new LinearTrackBar(System.Windows.Forms.Orientation.Horizontal);
                        break;
                    case TrackBarVisualiserType.LinearVertical:
                        this.Visualiser = new LinearTrackBar(System.Windows.Forms.Orientation.Vertical);
                        break;
                }
            }
        }
        private TrackBarVisualiserType _VisualiserType;
        /// <summary>
        /// Aktuální vizualizer. 
        /// Do této property lze nasetovat i konkrétní <see cref="TrackBarVisualiserType.Custom"/> vizualizer.
        /// </summary>
        public ITrackBarVisualiser Visualiser
        {
            get { return this._Visualiser; }
            set
            {
                if (value == null) return;
                if (this._Visualiser != null) this._Visualiser.TrackBar = null;
                this._Visualiser = value;
                this._VisualiserType = value.VisualiserType;
                this._Visualiser.TrackBar = this;
                this.ChildItemsReset();
            }
        }
        private ITrackBarVisualiser _Visualiser;
        #endregion
        #region Deklarace pro vizualizery : enum TrackBarVisualiserType, interface ITrackBarVisualiser
        /// <summary>
        /// Typy vizualizerů : vestavěné plus <see cref="Custom"/>
        /// </summary>
        public enum TrackBarVisualiserType
        {
            /// <summary>
            /// Vodorovný lineární
            /// </summary>
            LinearHorizontal,
            /// <summary>
            /// Svislý lineární
            /// </summary>
            LinearVertical,
            /// <summary>
            /// Externě dodaný
            /// </summary>
            Custom
        }
        /// <summary>
        /// Deklarace rozhraní pro vizualizery pro <see cref="GTrackBar"/>
        /// </summary>
        public interface ITrackBarVisualiser
        {
            /// <summary>
            /// Vlastník = <see cref="GTrackBar"/>. Pokud je vizualizer aktivně používán, pak tato reference není null.
            /// </summary>
            GTrackBar TrackBar { get; set; }
            /// <summary>
            /// Typ vizualizeru
            /// </summary>
            TrackBarVisualiserType VisualiserType { get; }
            /// <summary>
            /// Child interaktivní prvky
            /// </summary>
            IInteractiveItem[] ChildItems { get; }
            /// <summary>
            /// Umožní vizualizeru reagovat na interaktivitu jeho trackbaru
            /// </summary>
            /// <param name="e"></param>
            void AfterStateChanged(GInteractiveChangeStateArgs e);
            /// <summary>
            /// Zajistí vykreslení TrackBaru pomocí daného vizualizeru.
            /// Pozadí je již vykresleno.
            /// Pokud aktuální vizualizer využívá ChildItems prvky, pak i ty budou následně vykresleny.
            /// Pokud ChildItems prvky pokrývají celou plochu (pak budou kresleny i ony), a nic jiného vizualizer nenabízí, pak není třeba kreslit vlastní vizualizer.
            /// Hodnoty TrackBaru (data) si vizualizer čte přes property <see cref="TrackBar"/>.
            /// </summary>
            /// <param name="e"></param>
            /// <param name="absoluteBounds"></param>
            /// <param name="absoluteVisibleBounds"></param>
            /// <param name="drawMode"></param>
            void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode);
        }
        #endregion
        #region LinearTrackBar : konkrétní vizualizer pro vodorovný i svislý trackbar s lineární hodnotou
        /// <summary>
        /// LinearTrackBar : konkrétní vizualizer pro vodorovný i svislý trackbar s lineární hodnotou
        /// </summary>
        public class LinearTrackBar : ITrackBarVisualiser
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            public LinearTrackBar()
            {
                this.Orientation = System.Windows.Forms.Orientation.Horizontal;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="orientation"></param>
            public LinearTrackBar(System.Windows.Forms.Orientation orientation)
            {
                this.Orientation = orientation;
            }
            /// <summary>
            /// Orientace
            /// </summary>
            protected System.Windows.Forms.Orientation Orientation { get; private set; }
            /// <summary>
            /// Reference na TrackBar
            /// </summary>
            protected GTrackBar TrackBar { get; set; }
            /// <summary>
            /// Typ vizualizeru
            /// </summary>
            protected TrackBarVisualiserType VisualiserType { get { return (this.Orientation == System.Windows.Forms.Orientation.Horizontal ? TrackBarVisualiserType.LinearHorizontal : TrackBarVisualiserType.LinearVertical); } }
            /// <summary>
            /// Child prvky
            /// </summary>
            protected IInteractiveItem[] ChildItems { get { return new IInteractiveItem[0]; } }
            /// <summary>
            /// Řeší interaktivitu
            /// </summary>
            /// <param name="e"></param>
            protected void AfterStateChanged(GInteractiveChangeStateArgs e)
            {
                switch (e.ChangeState)
                {
                    case GInteractiveChangeState.MouseOver:
                        this.MouseOverPoint = e.MouseAbsolutePoint;
                        this.TrackBar.Repaint();
                        break;
                    case GInteractiveChangeState.LeftDown:
                        break;
                    case GInteractiveChangeState.LeftDragMoveBegin:
                        this.TrackBar.ValueDragOriginal = this.TrackBar.Value;
                        this.MouseOverPoint = null;
                        this.MouseDragPoint = e.MouseAbsolutePoint;
                        this.TrackBar.Repaint();
                        break;
                    case GInteractiveChangeState.LeftDragMoveStep:
                        this.MouseDragPoint = e.MouseAbsolutePoint;
                        if (this.MouseDragPoint.HasValue)
                            this.TrackBar.ValueDrag = this.GetValueForPoint(this.MouseDragPoint.Value);
                        this.TrackBar.Repaint();
                        break;
                    case GInteractiveChangeState.LeftDragMoveDone:
                        this.TrackBar.ValueDragOriginal = null;
                        this.MouseDragPoint = null;
                        this.TrackBar.Repaint();
                        break;
                    case GInteractiveChangeState.MouseLeave:
                        this.MouseOverPoint = null;
                        this.MouseDragPoint = null;
                        this.TrackBar.Repaint();
                        break;
                }
            }
            /// <summary>
            /// Vrací hodnotou pro daný bod
            /// </summary>
            /// <param name="point"></param>
            /// <returns></returns>
            protected Decimal GetValueForPoint(Point point)
            {
                return 0m;
            }
            /// <summary>
            /// Vrací bod pro danou hodnotu
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            protected Point GetPointForValue(Decimal value)
            {
                return Point.Empty;
            }
            /// <summary>
            /// Boud myši
            /// </summary>
            protected Point? MouseOverPoint { get; set; }
            /// <summary>
            /// Bod přetahování
            /// </summary>
            protected Point? MouseDragPoint { get; set; }
            /// <summary>
            /// Vykreslení
            /// </summary>
            /// <param name="e"></param>
            /// <param name="absoluteBounds"></param>
            /// <param name="absoluteVisibleBounds"></param>
            /// <param name="drawMode"></param>
            protected void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
            {
                Rectangle trackBounds = absoluteBounds.Enlarge(-6, -5, -7, -5);
                GPainter.DrawTrackTicks(e.Graphics, trackBounds, System.Windows.Forms.Orientation.Horizontal, this.TrackBar.TickCount);

                decimal value = this.TrackBar.Value;
                DecimalRange range = this.TrackBar.ValueTotal;


                int cy = trackBounds.Center().Y;

                Point valueCenter = new Point(this.MouseDragPoint.HasValue ? this.MouseDragPoint.Value.X : (int)(trackBounds.X + trackBounds.Width / 3), cy);
                GPainter.DrawTrackPointer(e.Graphics, valueCenter, new Size(14, 16), this.TrackBar.InteractiveState, TrackPointerType.OneSide, RectangleSide.None);
                
                decimal? ratio = range.GetRelativePositionAtValue(value);
                if (this.MouseOverPoint.HasValue)
                {

                    Color pointColor = (this.TrackBar.IsInInteractiveState(GInteractiveState.LeftDown, GInteractiveState.LeftDrag) ? Color.BlueViolet : Color.DimGray);
                    using (GPainter.GraphicsUseSmooth(e.Graphics))
                    {
                        e.Graphics.FillEllipse(Skin.Brush(pointColor), this.MouseOverPoint.Value.CreateRectangleFromCenter(8));
                    }
                }

            }

            GTrackBar ITrackBarVisualiser.TrackBar { get { return this.TrackBar; } set { this.TrackBar = value; } }
            TrackBarVisualiserType ITrackBarVisualiser.VisualiserType { get { return this.VisualiserType; } }
            IInteractiveItem[] ITrackBarVisualiser.ChildItems { get { return this.ChildItems; } }
            void ITrackBarVisualiser.AfterStateChanged(GInteractiveChangeStateArgs e) { this.AfterStateChanged(e); }
            void ITrackBarVisualiser.Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode) { this.Draw(e, absoluteBounds, absoluteVisibleBounds, drawMode); }
        }
        #endregion
    }
}
