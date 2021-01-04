using Asol.Tools.WorkScheduler.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// <see cref="TrackBar"/> : dovoluje nastavit hodnotu pomocí jezdce nebo jiného gripu, umístěného na ploše controlu
    /// </summary>
    public class TrackBar : InteractiveObject
    {
        #region Konstruktor a privátní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TrackBar() : base()
        {
            this._Layout = new PaintData(this);
            this._Value = 0m;
            this._ValueTotal = new DecimalRange(0m, 1m);
            this._InactiveFrame = new System.Windows.Forms.Padding(5);
            this._Orientation = System.Windows.Forms.Orientation.Horizontal;
            this._TrackPointerVisualSize = new Size(7, 15);
            this.ValueRoundMode = MidpointRounding.AwayFromZero;
            this.ToolTipShowOnDraw = true;
            this.Is.MouseMoveOver = true;
            this.Is.MouseDragMove = true;
            this.BackColor = Skin.TrackBar.BackColorTrack;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        public TrackBar(IInteractiveParent parent) : this()
        {
            this.Parent = parent;
        }
        private Decimal _Value;
        private DecimalRange _ValueTotal;
        private System.Windows.Forms.Orientation _Orientation;
        private Int32? _TickCount;
        private Size _TrackPointerVisualSize;
        private System.Windows.Forms.Padding _InactiveFrame;
        private PaintData _Layout;
        #endregion
        #region Data : Value, ValueTotal
        /// <summary>
        /// Aktuálně zobrazená hodnota.
        /// </summary>
        public Decimal Value
        {
            get { return this._Value; }
            set { this.SetValue(value, ProcessAction.ChangeAll, EventSourceType.ValueChange | EventSourceType.ApplicationCode); }
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
            set { this.SetValueTotal(value, ProcessAction.ChangeAll, EventSourceType.ValueRangeChange | EventSourceType.ApplicationCode); }
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

            // Pokud nyní je zdrojem akce událost ValueChange (tj. změna hodnoty je definitivní), a pokud známe ValueDragOriginal, 
            //  pak jako oldValue bereme tuto ValueDragOriginal:
            if (eventSource.HasAnyFlag(EventSourceType.ValueChange) && this.ValueDragOriginal.HasValue)
                oldValue = this.ValueDragOriginal.Value;

            Decimal newValue = value;
            if (IsAction(actions, ProcessAction.RecalcValue))
                newValue = this.ValueAlign(newValue);

            if (newValue == oldValue) return;    // No change = no reactions.

            this._Value = newValue;

            if (IsAction(actions, ProcessAction.PrepareInnerItems))
                this.ChildItemsReset();
            if (IsAction(actions, ProcessAction.CallChangingEvents))
                this.CallValueChanging(oldValue, newValue, eventSource);
            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallValueChanged(oldValue, newValue, eventSource);
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
        #endregion
        #region Zarovnání a Zaokrouhlování hodnoty
        /// <summary>
        /// Zarovná hodnotu do aktuálního rozmezí
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual decimal ValueAlign(decimal value)
        {
            decimal valueRounded = this.ValueRound(value);
            decimal valueAligned = this._ValueTotal.Align(valueRounded);
            return valueAligned;
        }
        /// <summary>
        /// Vrací dodanou hodnotu po zaokrouhlení
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual decimal ValueRound(decimal value)
        {
            if (this.ValueRoundDecimals.HasValue)
                value = Math.Round(value, this.ValueRoundDecimals.Value, this.ValueRoundMode);
            if (this.ValueRounder != null)
                value = this.ValueRounder(value);
            return value;
        }
        /// <summary>
        /// Počet desetinných míst pro zaokrouhlování hodnoty, null = bez zaokrouhlení
        /// </summary>
        public int? ValueRoundDecimals { get; set; }
        /// <summary>
        /// Režim zaokrouhlení hodnoty
        /// </summary>
        public MidpointRounding ValueRoundMode { get; set; }
        /// <summary>
        /// Externě dodaná metoda, která zaokrouhluje hodnotu.
        /// Metoda je volána při každé změně hodnoty.
        /// Metoda dostává hodnotu, kterou interaktivně zadává uživatel / programově vepisuje kód.
        /// Hodnota je po prvotním zaokrouhlení podle <see cref="ValueRoundDecimals"/> a <see cref="ValueRoundMode"/>.
        /// Tuto hodnotu zaokrouhluje zdejší metoda <see cref="ValueRounder"/>.
        /// Její výsledek je zarovnán do mezí <see cref="ValueTotal"/> a vložen do <see cref="Value"/> a zobrazen.
        /// </summary>
        public Func<decimal, decimal> ValueRounder;
        #endregion
        #region Properties pro řízení vzhledu
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
        protected Int32? TickCount
        {
            get { return this._TickCount; }
            set { this._TickCount = value; this.Repaint(); }
        }
        /// <summary>
        /// Vizuální velikost prvku TrackPointer, nezávislá na orientaci.
        /// Odpovídá fyzické velikosti při orientaci Horizontal.
        /// Nejmenší velikost je 7x7. Počet pixelů musí být lichý, 
        /// aby byl 1px na středu a pak shodný počet pixelů na obou stranách od středu stejně = aby track byl symetrický.
        /// </summary>
        protected Size TrackPointerVisualSize
        {
            get { return this._TrackPointerVisualSize; }
            set
            {
                int w = _GetVisualTrackPixels(value.Width);
                int h = _GetVisualTrackPixels(value.Height);
                this._TrackPointerVisualSize = new Size(w, h);
                this.Repaint();
            }
        }
        private static int _GetVisualTrackPixels(int pixels)
        {
            if (pixels < 7) return 7;
            int add = ((pixels + 1) % 2);         // Sudé číslo bude mít add = 1; liché číslo = 0
            return pixels + add;
        }
        /// <summary>
        /// Vnitřní neaktivní okraj (prostor mezi Bounds a prostorem, v němž je aktivní TrackBar)
        /// </summary>
        protected System.Windows.Forms.Padding InactiveFrame
        {
            get { return this._InactiveFrame; }
            set { this._InactiveFrame = value; this.Repaint(); }
        }
        /// <summary>
        /// Vzhled TrackBaru
        /// </summary>
        public PaintData Layout { get { return this._Layout; } }
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
            TrackBarAreaType partType = this.GetPartType(e);
            switch (e.ChangeState)
            {
                case GInteractiveChangeState.MouseEnter:
                    this.MouseEnter(partType, e);
                    break;
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
                case GInteractiveChangeState.LeftUp:
                    this.LeftUp(partType, e);
                    break;
                case GInteractiveChangeState.MouseLeave:
                    this.MouseLeave(partType, e);
                    break;
            }
            base.AfterStateChanged(e);
        }
        /// <summary>
        /// Provede obsluhu MouseEnter
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void MouseEnter(TrackBarAreaType partType, GInteractiveChangeStateArgs e)
        {
            this.MouseOverPartChange(null, partType, e);
            this.ToolTipPrepare(e, false, false);
        }
        /// <summary>
        /// Provede obsluhu MouseOver
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void MouseOver(TrackBarAreaType partType, GInteractiveChangeStateArgs e)
        {
            this.MouseOverPoint = e.MouseAbsolutePoint;
            if (partType != this.LastMouseOverPart)
            {
                this.MouseOverPartChange(this.LastMouseOverPart, partType, e);
                this.LastMouseOverPart = partType;
            }
            this.Repaint();
        }
        /// <summary>
        /// Provede obsluhu MouseLeave
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void MouseLeave(TrackBarAreaType partType, GInteractiveChangeStateArgs e)
        {
            this.MouseOverPartChange(this.LastMouseOverPart, null, e);
            this.LastMouseOverPart = TrackBarAreaType.None;
            this.MouseDragOffset = null;
            this.MouseOverPoint = null;
            this.Repaint();
        }
        /// <summary>
        /// Řeší změnu pozice myši nad prvky TrackBaru
        /// </summary>
        /// <param name="oldPartType"></param>
        /// <param name="newPartType"></param>
        /// <param name="e"></param>
        protected virtual void MouseOverPartChange(TrackBarAreaType? oldPartType, TrackBarAreaType? newPartType, GInteractiveChangeStateArgs e)
        {

        }
        /// <summary>
        /// Provede obsluhu LeftDown
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void LeftDown(TrackBarAreaType partType, GInteractiveChangeStateArgs e)
        {
            this.MouseOverPoint = null;
            this.ValueDragOriginal = this.Value;
            switch (partType)
            {
                case TrackBarAreaType.NonActive:
                case TrackBarAreaType.Area:
                    // Kliknuto do aktivní plochy, ale ne do oblasti TrackPointeru:
                    //  => okamžitě přemístíme TrackPointeru na daný bod, a budeme očekávat Drag and Move:
                    this.ValueDrag = this.GetTrackValue(e);
                    this.MouseDragOffset = new Point(0, 0);
                    break;
                case TrackBarAreaType.Pointer:
                    // Kliknuto do prostoru TrackPointeru:
                    //  => TrackPointer nikam nepřemísťujeme, určíme jenom offset pozice myši od TrackPointeru, a počkáme jestli uživatel sám provede Drag and Move:
                    this.MouseDragOffset = e.MouseRelativePoint.Value.Sub(this.TrackPoint);
                    break;
            }
            this.LastMouseOverPart = partType;
            this.ToolTipPrepare(e, true, true);
            this.Repaint();
        }
        /// <summary>
        /// Provede obsluhu MouseDrag Begin
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void LeftDragBegin(TrackBarAreaType partType, GInteractiveChangeStateArgs e)
        {
            this.MouseOverPoint = null;
            this.ToolTipPrepare(e, true, true);
            this.Repaint();
        }
        /// <summary>
        /// Provede obsluhu MouseDrag Move
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void LeftDragMove(TrackBarAreaType partType, GInteractiveChangeStateArgs e)
        {
            if (!this.MouseDragOffset.HasValue || !e.MouseRelativePoint.HasValue) return;

            Point dragPoint = e.MouseRelativePoint.Value.Sub(this.MouseDragOffset.Value);
            this.ValueDrag = this.GetValueForPoint(dragPoint);
            this.ToolTipPrepare(e, true, true);
            this.Repaint();
        }
        /// <summary>
        /// Provede obsluhu MouseDrag Drop
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void LeftDragDrop(TrackBarAreaType partType, GInteractiveChangeStateArgs e)
        {
            this.Value = this.ValueDrag;
            this.ValueDragOriginal = null;
            this.MouseDragOffset = null;
            this.MouseOverPoint = e.MouseAbsolutePoint;
            this.ToolTipPrepare(e, false, true);
            this.Repaint();
        }
        /// <summary>
        /// Provede obsluhu MouseUp.
        /// Tato metoda není volaná po skončení procesu Drag and Move, pouze po LeftDown bez následného Drag.
        /// </summary>
        /// <param name="partType"></param>
        /// <param name="e"></param>
        protected virtual void LeftUp(TrackBarAreaType partType, GInteractiveChangeStateArgs e)
        {
            this.Value = this.ValueDrag;
            this.ValueDragOriginal = null;
            this.MouseDragOffset = null;
            this.MouseOverPoint = e.MouseAbsolutePoint;
            this.ToolTipPrepare(e, false, true);
            this.Repaint();
        }
        /// <summary>
        /// Připraví data pro ToolTip
        /// </summary>
        /// <param name="e"></param>
        /// <param name="isValueDrag"></param>
        /// <param name="isInstant"></param>
        protected virtual void ToolTipPrepare(GInteractiveChangeStateArgs e, bool isValueDrag, bool isInstant)
        {
            if (isValueDrag && !this.ToolTipShowOnDraw) return;

            string title = this.ToolTipTitle;
            string text = this.ToolTipText;
            if (String.IsNullOrEmpty(title) && String.IsNullOrEmpty(text)) return;

            e.ToolTipData.TitleText = title;
            e.ToolTipData.InfoText = text;
            if (isInstant)
                e.ToolTipData.AnimationType = (isValueDrag ? TooltipAnimationType.Instant : TooltipAnimationType.InstantInFadeOut);
        }
        /// <summary>
        /// Metoda vrátí oblast, ve které se pohybuje myš
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual TrackBarAreaType GetPartType(GInteractiveChangeStateArgs e)
        {
            Point? mouseRelativePoint = e.MouseRelativePoint;              // Souřadnice myši relativně k this, tj. hodnota 0/0 znamená levý horní roh TrackBaru
            if (!mouseRelativePoint.HasValue) return TrackBarAreaType.None;
            Point mouse = mouseRelativePoint.Value;

            Rectangle trackPointerBounds = this.TrackPointerActiveBounds;
            if (trackPointerBounds.Contains(mouse)) return TrackBarAreaType.Pointer;

            Rectangle activeBounds = this.ActiveBounds;
            if (activeBounds.Contains(mouse)) return TrackBarAreaType.Area;

            return TrackBarAreaType.NonActive;
        }
        /// <summary>
        /// Metoda vrátí bod nejbližší k bodu (e.MouseRelativePoint), který leží na dráze posuvníku <see cref="TrackLineBounds"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual decimal GetTrackValue(GInteractiveChangeStateArgs e)
        {
            Rectangle trackLine = this.TrackLineBounds;
            Point trackPoint = (e.MouseRelativePoint.HasValue ? e.MouseRelativePoint.Value.FitInto(trackLine) : trackLine.Location);
            decimal value = this.GetValueForPoint(trackPoint, trackLine);
            return value;
        }
        /// <summary>
        /// Bod myši při MouseOver. Toto je absolutní souřadnice myši.
        /// Může posloužit pro vykreslení "stínu"
        /// </summary>
        protected Point? MouseOverPoint { get; set; }
        /// <summary>
        /// Offset myši proti Pointeru při začátku Drag and Drop (= MousePoint - TrackPoint)
        /// </summary>
        protected Point? MouseDragOffset { get; set; }
        /// <summary>
        /// Část prostoru, nad nímž byla myš při posledním MouseOver
        /// </summary>
        protected TrackBarAreaType LastMouseOverPart { get; set; }
        #endregion
        #region ToolTip, Text
        /// <summary>
        /// Text vypisovaný v dolním okraji prvku
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Titulek tooltipu
        /// </summary>
        public string ToolTipTitle { get; set; }
        /// <summary>
        /// Text tooltipu
        /// </summary>
        public string ToolTipText { get; set; }
        /// <summary>
        /// true = Zobrazovat text <see cref="ToolTipText"/> celou dobu přetahování (když je myš stisknutá), dynamický;
        /// false = zobrazovat jen přoi najetí myší, statický
        /// (Pokud je <see cref="ToolTipText"/> prázdný, nezobrazí se).
        /// </summary>
        public bool ToolTipShowOnDraw { get; set; }
        #endregion
        #region Ikonky

        #endregion
        #region Výpočty souřadnic jednotlivých částí TrackBaru, relativně k TrackBar.Bounds, podle hodnoty Value
        /// <summary>
        /// Vizuální velikost prvku TrackPointer, reálná při aktuální orientaci
        /// </summary>
        protected Size TrackPointerCurrentVisualSize
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
        /// Souřadnice vizuální části = zde se vykresluje podklad TrackBaru.
        /// Ve směru trackbaru odpovídá <see cref="TrackLineBounds"/>, v neaktivním směru odpovídá <see cref="ActiveBounds"/>.
        /// </summary>
        protected Rectangle TrackBounds
        {
            get
            {
                Rectangle trackLine = this.TrackLineBounds;
                Rectangle activeBounds = this.ActiveBounds;

                switch (this.Orientation)
                {
                    case System.Windows.Forms.Orientation.Horizontal:
                        return new Rectangle(trackLine.X, activeBounds.Y, trackLine.Width, activeBounds.Height);
                    case System.Windows.Forms.Orientation.Vertical:
                        return new Rectangle(activeBounds.X, trackLine.Y, activeBounds.Width, trackLine.Height);
                }

                return activeBounds;
            }
        }
        /// <summary>
        /// Obsahuje souřadnice prostoru, v němž se může pohybovat aktivní bod trackbaru <see cref="TrackPoint"/>, relativní = v souřadnicích TrackBar.Bounds.
        /// U horizontálního TrackBaru je to vodorovná čára s Height = 0; u vertikálního je to svislá čára s Width = 0.
        /// </summary>
        protected Rectangle TrackLineBounds
        {
            get
            {
                // Velikost TrackPointeru je vždy liché číslo, takže 2 * (int)(velikost / 2) je o 1px menší než velikost.
                // Jinými slovy: s1 = počet pixelů před linkou, pak je 1 px linka, a pak je s1 = počet pixelů za linkou:
                int trackSize = this.TrackPointerVisualSize.Width; // Size.Width se používá jako šířka TrackPointeru i kdyby byl na výšku (pak se použije na ose Y = jako fyzická výška)
                int s1 = trackSize / 2;                            // Část TrackPointeru před jeho ideální polovinou

                Rectangle activeBounds = this.ActiveBounds;
                Point center = activeBounds.Center();

                switch (this.Orientation)
                {
                    case System.Windows.Forms.Orientation.Horizontal:
                        int w = activeBounds.Width - 3 - 2 * s1;
                        int x0 = activeBounds.X + s1 + 1;
                        return new Rectangle(x0, center.Y, w, 0);
                    case System.Windows.Forms.Orientation.Vertical:
                        int h = activeBounds.Height - 3 - 2 * s1;
                        int y0 = activeBounds.Y + s1 + 1;
                        return new Rectangle(center.X, y0, 0, h);
                }

                return new Rectangle(center.X, center.Y, 0, 0);
            }
        }
        /// <summary>
        /// Relativní souřadnice vnitřního aktivního prostoru, relativní = vzhledem k TrackBaru.
        /// Pokud tedy TrackBar má souřadnice { 100, 20, 200, 40 } a má <see cref="TrackBar.InactiveFrame"/> = 4, 
        /// pak <see cref="ActiveBounds"/> = { 4, 4, 192, 32 }.
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
        #region Převody hodnoty TrackBaru na souřadnice a převody reverzní
        /// <summary>
        /// Vrací bod pro danou hodnotu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual Point GetPointForValue(Decimal value)
        {
            Decimal? ratio = (this._ValueTotal != null ? this._ValueTotal.GetRelativePositionAtValue(value) : (Decimal?)null);
            return this.GetTrackPoint(ratio);
        }
        /// <summary>
        /// Metoda vrátí souřadnici bodu (relativní = v souřadnicích TrackBar.Bounds) pro danou relativní hodnotu 0 až 1.
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        protected Point GetTrackPoint(Decimal? ratio)
        {
            return this.GetTrackPoint(ratio, this.TrackLineBounds);
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
        /// Vrací hodnotou pro daný bod a aktuální TrackLine
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected virtual Decimal GetValueForPoint(Point point)
        {
            return this.GetValueForPoint(point, this.TrackLineBounds);
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
                DecimalRange.CreateFromBeginSize(0, trackLine.Height).GetRelativePositionAtValue(trackLine.Bottom - point.Y));
            decimal r = (ratio.HasValue ? (ratio.Value < 0m ? 0m : (ratio.Value > 1m ? 1m : ratio.Value)) : 0m);
            return this._ValueTotal.GetValueAtRelativePosition(r);
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
            GPainter.DrawTrackBar(e.Graphics, absoluteBounds, this.Layout);
        }
        /// <summary>
        /// Metoda je volána v procesu kreslení TrackBaru. Umožní vykreslit text nebo ikonky.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="absoluteBounds"></param>
        protected void PaintTextData(Graphics graphics, Rectangle absoluteBounds)
        {
            // GPainter.DrawString(graphics, absoluteBounds, "TRACKER", Skin.Brush(Color.Black), FontInfo.DefaultBold, ContentAlignment.BottomCenter);
        }
        /// <summary>
        /// Třída pro předání dat o Layoutu do kreslení
        /// </summary>
        public class PaintData : ITrackBarPaintData
        {
            #region Konstruktor
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="trackBar"></param>
            internal PaintData(TrackBar trackBar)
            {
                this._TrackBar = trackBar;
                this.SetDefaults();
            }
            private TrackBar _TrackBar;
            #endregion
            #region Layout data
            /// <summary>
            /// Orientace TrackBaru
            /// </summary>
            public Orientation Orientation { get { return this._TrackBar.Orientation; } set { this._TrackBar.Orientation = value; } }
            /// <summary>
            /// Počet vykreslovaných úseků Ticků, null = 0 = žádný.
            /// Z hlediska logiky zadávání je počet linek ticků = (<see cref="TickCount"/> - 1).
            /// Jde o počet úseků mezi Ticky, přičemž je nutno uvažovat i krajní linky.
            /// Zadáme-li tedy <see cref="TickCount"/> = 10, bude vykresleno 10 úseků: linka vlevo, pak 9x úsek + tick, a na konci úsek + linka vpravo.
            /// </summary>
            public Int32? TickCount { get { return this._TrackBar.TickCount; } set { this._TrackBar.TickCount = value; } }
            /// <summary>
            /// Vizuální velikost prvku TrackPointer, nezávislá na orientaci.
            /// Odpovídá fyzické velikosti při orientaci Horizontal.
            /// Nejmenší velikost je 7x7. Počet pixelů musí být lichý, 
            /// aby byl 1px na středu a pak shodný počet pixelů na obou stranách od středu stejně = aby track byl symetrický.
            /// </summary>
            public Size TrackPointerVisualSize { get { return this._TrackBar.TrackPointerVisualSize; } set { this._TrackBar.TrackPointerVisualSize = value; } }
            /// <summary>
            /// Vnitřní neaktivní okraj (prostor mezi Bounds a prostorem, v němž je aktivní TrackBar)
            /// </summary>
            public Padding InactiveFrame { get { return this._TrackBar.InactiveFrame; } set { this._TrackBar.InactiveFrame = value; } }
            /// <summary>
            /// Typ kreslených Ticků
            /// </summary>
            public TrackBarTickType TickType { get; set; }
            /// <summary>
            /// Barva ticků
            /// </summary>
            public Color? TickColor { get; set; }
            /// <summary>
            /// Barva krajních čar
            /// </summary>
            public Color? EndBarColor { get; set; }
            /// <summary>
            /// Barva TrackLine = čáry okolo linie trackbaru
            /// </summary>
            public Color? TrackLineColor { get; set; }
            /// <summary>
            /// Barva pozadí TrackLine, pokud je Solid
            /// </summary>
            public Color? TrackBackColor { get; set; }
            /// <summary>
            /// Barva aktuální (=dosažené) hodnoty pozadí TrackLine, pokud je Solid
            /// </summary>
            public Color? TrackActiveBackColor { get; set; }
            /// <summary>
            /// Barva aktuální (=od dosažené do nejvyšší) hodnoty pozadí TrackLine, pokud je Solid
            /// </summary>
            public Color? TrackInactiveBackColor { get; set; }
            /// <summary>
            /// Barva pozadí TrackPointu = ukazatele
            /// </summary>
            public Color? TrackPointBackColor { get; set; }
            /// <summary>
            /// Barva linky TrackPointu = ukazatele
            /// </summary>
            public Color? TrackPointLineColor { get; set; }
            /// <summary>
            /// Barva pozadí TrackPointu = ukazatele, ve stavu MouseOver
            /// </summary>
            public Color? TrackPointMouseOverBackColor { get; set; }
            /// <summary>
            /// Barva pozadí TrackPointu = ukazatele, ve stavu MouseOver
            /// </summary>
            public Color? TrackPointMouseDownBackColor { get; set; }
            /// <summary>
            /// Typ track line
            /// </summary>
            public TrackBarLineType TrackLineType { get; set; }
            /// <summary>
            /// Barvy použité pro TrackLine typu <see cref="TrackBarLineType.ColorBlendLine"/>.
            /// Pokud je null, pak takový TrackLine bude mít defaultní barevný přechod.
            /// </summary>
            IEnumerable<Tuple<float, Color>> ColorBlend { get; set; }
            /// <summary>
            /// Nastaví defaulty
            /// </summary>
            protected void SetDefaults()
            {
                this.TrackLineType = TrackBarLineType.Line;
                this.TickType = TrackBarTickType.Standard;
            }
            #endregion
            #region ITrackBarPaintData
            Orientation ITrackBarPaintData.Orientation { get { return this._TrackBar.Orientation; } }
            GInteractiveState ITrackBarPaintData.InteractiveState { get { return this._TrackBar.InteractiveState; } }
            TrackBarAreaType ITrackBarPaintData.CurrentMouseArea { get { return this._TrackBar.LastMouseOverPart; } }
            Color ITrackBarPaintData.BackColor { get { return this._TrackBar.BackColor.Value; } }
            Rectangle ITrackBarPaintData.ActiveBounds { get { return this._TrackBar.ActiveBounds; } }
            Rectangle ITrackBarPaintData.TrackBounds { get { return this._TrackBar.TrackBounds; } }
            Rectangle ITrackBarPaintData.TrackLineBounds { get { return this._TrackBar.TrackLineBounds; } }
            Point ITrackBarPaintData.TrackPoint { get { return this._TrackBar.TrackPoint; } }
            Size ITrackBarPaintData.TrackSize { get { return this._TrackBar.TrackPointerCurrentVisualSize; } }
            int? ITrackBarPaintData.TickCount { get { return this._TrackBar.TickCount; } }
            TrackBarTickType ITrackBarPaintData.TickType { get { return this.TickType; } }
            Color? ITrackBarPaintData.TickColor { get { return this.TickColor; } }
            Color? ITrackBarPaintData.EndBarColor { get { return this.EndBarColor; } }
            Color? ITrackBarPaintData.TrackLineColor { get { return this.TrackLineColor; } }
            Color? ITrackBarPaintData.TrackBackColor { get { return this.TrackBackColor; } }
            Color? ITrackBarPaintData.TrackActiveBackColor { get { return this.TrackActiveBackColor; } }
            Color? ITrackBarPaintData.TrackInactiveBackColor { get { return this.TrackInactiveBackColor; } }
            Color? ITrackBarPaintData.TrackPointBackColor { get { return this.TrackPointBackColor; } }
            Color? ITrackBarPaintData.TrackPointLineColor { get { return this.TrackPointLineColor; } }
            Color? ITrackBarPaintData.TrackPointMouseOverBackColor { get { return this.TrackPointMouseOverBackColor; } }
            Color? ITrackBarPaintData.TrackPointMouseDownBackColor { get { return this.TrackPointMouseDownBackColor; } }
            TrackBarLineType ITrackBarPaintData.TrackLineType { get { return this.TrackLineType; } }
            IEnumerable<Tuple<float, Color>> ITrackBarPaintData.ColorBlend { get { return this.ColorBlend; } }
            void ITrackBarPaintData.PaintTextData(Graphics graphics, Rectangle absoluteBounds) { this._TrackBar.PaintTextData(graphics, absoluteBounds); }
            #endregion

        }
        #endregion
    }
}
