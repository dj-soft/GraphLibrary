using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Data
{
    #region class IntervalArray : chain (of PrevNextItem) with items as interval of TValue (Begin ÷ End)
    /// <summary>
    /// IntervalArray : chain (of PrevNextItem) with items as interval of TValue (Begin ÷ End)
    /// </summary>
    /// <typeparam name="TValue">Type of Value for Begin and End of individual interval</typeparam>
    public class IntervalArray<TValue> : IntervalBase, IEnumerable<Interval<TValue>>, IDeepCloneable
        where TValue : IComparable<TValue>, IComparable
    {
        #region Constructor and public properties, First item
        public IntervalArray()
        {
            this.First = null;
        }
        public IntervalArray(TValue begin, TValue end)
        {
            this.First = new PrevNextItem<Interval<TValue>>(new Interval<TValue>(begin, end));
        }
        public IntervalArray(Interval<TValue> first)
        {
            this.First = (first != null ? new PrevNextItem<Interval<TValue>>(first) : null);
        }
        public override string ToString()
        {
            return this.Text;
        }
        public string Text
        { 
            get
            {
                string text = "";
                PrevNextItem<Interval<TValue>> item = this.First;
                if (item != null)
                {
                    item = item.First;
                    while (item != null)
                    {
                        bool isThis = Object.ReferenceEquals(item, this);
                        text += (text.Length == 0 ? "" : " - ") + (isThis ? "{" : "") + item.ToString() + (isThis ? "}" : "");
                        item = item.Next;
                    }
                }
                return text;
            }
        }
        /// <summary>
        /// true for empty interval
        /// </summary>
        public bool IsEmpty { get { return (this.First == null || (!this.First.Value.IsFilled && !this.First.HasPrev && !this.First.HasNext)); } }
        /// <summary>
        /// First item in array, has lowest values.
        /// Can be null.
        /// </summary>
        protected PrevNextItem<Interval<TValue>> First;
        #endregion
        #region Add, AddRange method
        /// <summary>
        /// Add into this array new interval.
        /// Merge it into existing item, or add itnerval as new item at correct position.
        /// This array is always sorted by the Begin value, and neither intervals do not overlap or touch.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public void Add(TValue begin, TValue end)
        {
            this._Add(new Interval<TValue>(begin, end));
        }
        /// <summary>
        /// Add into this array new interval.
        /// Merge it into existing item, or add itnerval as new item at correct position.
        /// This array is always sorted by the Begin value, and neither intervals do not overlap or touch.
        /// </summary>
        /// <param name="item"></param>
        public void Add(Interval<TValue> item)
        {
            this._Add(item);
        }
        /// <summary>
        /// Add into this array new array of interval.
        /// Merge it into existing item, or add itnerval as new item at correct position.
        /// This array is always sorted by the Begin value, and neither intervals do not overlap or touch.
        /// </summary>
        /// <param name="array"></param>
        public void AddArray(IntervalArray<TValue> array)
        {
            PrevNextItem<Interval<TValue>> item = array.First;
            while (item != null)
            {
                this._Add(item.Value);
                item = item.Next;
            }
        }
        /// <summary>
        /// Add into this array new array of interval.
        /// Merge it into existing item, or add itnerval as new item at correct position.
        /// This array is always sorted by the Begin value, and neither intervals do not overlap or touch.
        /// </summary>
        /// <param name="array"></param>
        public void AddRange(IEnumerable<Interval<TValue>> items)
        {
            if (items != null)
            {
                foreach (Interval<TValue> item in items)
                    this._Add(item);
            }
        }
        /// <summary>
        /// Add into this array new interval.
        /// Merge it into existing item, or add itnerval as new item at correct position.
        /// This array is always sorted by the Begin value, and neither intervals do not overlap or touch.
        /// </summary>
        /// <param name="addItem"></param>
        private void _Add(Interval<TValue> addItem)
        {
            if (addItem == null || !addItem.IsFilled) return;

            // First item:
            if (this.First == null || !this.First.Value.IsFilled)
            {
                this.First = new PrevNextItem<Interval<TValue>>(addItem.ValueClone);
                return;
            }

            // Always scan only from First item to Next items, do not scan Prev items (First is first!)
            IntervalRelation? relation = null;
            IntervalRelation? relationNext = null;
            
            // Add/Merge with current status:
            PrevNextItem<Interval<TValue>> scanItem = this.First;
            bool scanIsFirst = true;
            while (scanItem != null)
            {
                // Relation from scanItem to addItem:
                if (!relation.HasValue) relation = scanItem.Value.GetRelationFor(addItem);
               
                // If addItem is whole contained in scanItem, no need any merge, and Add will ends:
                if (Interval<TValue>.Contains(relation.Value))
                    break;

                if (Interval<TValue>.IsWholeBefore(relation.Value))
                {   // If addItem is whole before scanItem, then insert addItem before scanItem, and Add will ends:
                    scanItem.InsertBefore(addItem);
                    if (scanIsFirst)
                        this.First = scanItem.First;
                    break;
                }

                if (!relationNext.HasValue) relationNext = (scanItem.HasNext ? scanItem.Next.Value.GetRelationFor(addItem) : IntervalRelation.None);

                if (Interval<TValue>.CanMergeWith(relation.Value))
                {   // If scanItem can be merged with addItem (addItem is partially over scanItem):
                    if (Interval<TValue>.CanMergeWith(relationNext.Value))
                    {
                        // and when addItem is also mergable with scanItem.Next (addItem is over booth items) => merge scanItem.Next into scanItem:
                        scanItem.MergeWithNext();
                        // In next step will be again processed same scanItem, but with new values, thus with newly detected relations:
                        relation = null;
                        relationNext = null;
                        continue;
                    }
                    else
                    {   // addItem can not be merged with Next item: only merge with scanItem item, and exit:
                        scanItem.MergeWith(addItem);
                        break;
                    }
                }
                // addItem can not be merged with scanItem. addItem is not WholeBefore (this is first test), then must be WholeAfter:
                else if (Interval<TValue>.IsWholeAfter(relation.Value))
                {   // Tested item is whole AFTER current scan item:
                    if (!scanItem.HasNext)
                    {   // scanItem has not Next item, then addItem will be inserted after scanItem, and exit:
                        scanItem.InsertAfter(addItem);
                        break;
                    }
                    // scanItem has Next item, but addItem is not mergable with scanItem.
                    // Then addItem will be in next step processed in relation to scanItem.Next item:
                    scanItem = scanItem.Next;
                    relation = relationNext;
                    relationNext = null;
                    scanIsFirst = false;
                    continue;
                }
                // When algorithm occurs here, then any error in agorihm exists!
                scanItem = scanItem.Next;
                relation = relationNext;
                relationNext = null;
                scanIsFirst = false;
                continue;
            }
        }
        #endregion
        #region Search for used / unused space 
        public Interval<TValue> SearchForSpace(TValue searchFrom, TValue size, Func<TValue, TValue, TValue> addFunction)
        {
            // Detection of direction: scan Up (when size is positive value) or Down (size is negative value):
            TValue end = addFunction(searchFrom, size);              // end = (searchFrom + size)
            int direction = searchFrom.CompareTo(end);               // Compare(searchFrom - end): +1 = Down; -1 = Up; 0 = zero size !

            if (direction < 0)
                return this._SearchForSpaceUp(searchFrom, size, end, addFunction);
            else if (direction > 0)
                return this._SearchForSpaceDown(searchFrom, size, end, addFunction);

            return new Interval<TValue>(searchFrom, searchFrom);     // When size = 0, then end = searchFrom and we return empty interval at searchFrom value.
        }
        /// <summary>
        /// Search for free space for positive size (=search from First to Last item)
        /// </summary>
        /// <param name="searchFrom">Start value for search</param>
        /// <param name="size">Search for size. Value is positive, we search Upward (from First to Last).</param>
        /// <param name="end">This is value of (searchFrom + size)</param>
        /// <param name="addFunction">Function for summary (a + b = result)</param>
        /// <returns></returns>
        private Interval<TValue> _SearchForSpaceUp(TValue searchFrom, TValue size, TValue end, Func<TValue, TValue, TValue> addFunction)
        {   // Search up, from lower interval (this.First), from (searchFrom + size), to Next items... :
            Interval<TValue> searchFor = new Interval<TValue>(searchFrom, end);
            PrevNextItem<Interval<TValue>> scanItem = this.First;
            while (scanItem != null)
            {
                IntervalRelation relation = scanItem.Value.GetRelationFor(searchFor);
                if (relation.HasFlag(IntervalRelation.EndBeforeBegin) || relation.HasFlag(IntervalRelation.EndOnBegin))
                    break;
                if (relation.HasFlag(IntervalRelation.BeginBeforeBegin) || relation.HasFlag(IntervalRelation.BeginOnBegin) || relation.HasFlag(IntervalRelation.BeginInner))
                    searchFor = new Interval<TValue>(scanItem.Value.End, addFunction(scanItem.Value.End, size));
                scanItem = scanItem.Next;
            }
            return searchFor;
        }
        /// <summary>
        /// Search for free space for negative size (=search from Last to First item)
        /// </summary>
        /// <param name="searchFrom">Start value for search</param>
        /// <param name="size">Search for size. Value is negative, we search Downward (from Last to First).</param>
        /// <param name="end">This is value of (searchFrom + size)</param>
        /// <param name="addFunction">Function for summary (a + b = result)</param>
        /// <returns></returns>
        private Interval<TValue> _SearchForSpaceDown(TValue searchFrom, TValue size, TValue end, Func<TValue, TValue, TValue> addFunction)
        {   // Search down, from upper interval (this.First.Last), from (searchFrom + size), to Prev items... :
            Interval<TValue> searchFor = new Interval<TValue>(end, searchFrom);          // size is negative value, end is lower than searchFrom. But we need "real" interval, where Begin < End.
            PrevNextItem<Interval<TValue>> scanItem = (this.First != null ? this.First.Last : null);         // Scan from Last item
            while (scanItem != null)
            {
                IntervalRelation relation = scanItem.Value.GetRelationFor(searchFor);
                if (relation.HasFlag(IntervalRelation.BeginAfterEnd) || relation.HasFlag(IntervalRelation.BeginOnEnd))
                    break;
                if (relation.HasFlag(IntervalRelation.EndAfterEnd) || relation.HasFlag(IntervalRelation.EndOnEnd) || relation.HasFlag(IntervalRelation.EndOnBegin))
                    searchFor = new Interval<TValue>(addFunction(scanItem.Value.Begin, size), scanItem.Value.Begin);
                scanItem = scanItem.Prev;
            }
            return searchFor;
        }
        #endregion
        #region Helper comparators
        /// <summary>
        /// Returns true when "a" is lower than "b"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool IsALowerThanB(TValue a, TValue b) { return (a.CompareTo(b) < 0); }
        /// <summary>
        /// Returns true when "a" is lower or equal to "b"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool IsALowerOrEqualToB(TValue a, TValue b) { return (a.CompareTo(b) <= 0); }
        /// <summary>
        /// Returns true when "a" is equal to "b"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool IsAEqualToB(TValue a, TValue b) { return (a.CompareTo(b) == 0); }
        /// <summary>
        /// Returns true when "a" is higher or equal to "b"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool IsAHigherOrEqualToB(TValue a, TValue b) { return (a.CompareTo(b) >= 0); }
        /// <summary>
        /// Returns true when "a" is higher than "b"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool IsAHigherThanB(TValue a, TValue b) { return (a.CompareTo(b) > 0); }
        #endregion
        #region Summary
        /// <summary>
        /// Return an IntervalArray as Sum(items).
        /// Scan each array, and sum all its Interval into result summary.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static IntervalArray<TValue> Summary(IEnumerable<IntervalArray<TValue>> items)
        {
            IntervalArray<TValue> sum = new IntervalArray<TValue>();
            if (items != null)
            {
                foreach (IntervalArray<TValue> item in items)
                    sum.AddArray(item);
            }
            return sum;
        }
        #endregion
        #region IEnumerable and IDeepCloneable members
        IEnumerator<Interval<TValue>> IEnumerable<Interval<TValue>>.GetEnumerator() { return this._GetValues().GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this._GetValues().GetEnumerator(); }
        private List<Interval<TValue>> _GetValues()
        {
            if (this.IsEmpty) return new List<Interval<TValue>>();
            return this.First.Items;
        }
        /// <summary>
        /// Get a deep (=full) clone of this instance.
        /// </summary>
        public IntervalArray<TValue> DeepClone
        {
            get
            {
                IntervalArray<TValue> clone = new IntervalArray<TValue>();
                PrevNextItem<Interval<TValue>> first = this.First;
                if (first != null)
                    clone.First = first.DeepClone;
                return clone;
            }
        }
        /// <summary>
        /// Get a deep (=full) clone of this instance.
        /// </summary>
        object IDeepCloneable.DeepClone { get { return this.DeepClone; } }
        #endregion
    }
    #endregion
    #region class Interval : one interval with Begin, End values
    /// <summary>
    /// Interval : one interval with Begin, End values
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class Interval<TValue> : IntervalBase, IMergeable, IValueCloneable
        where TValue : IComparable<TValue>, IComparable
    {
        #region Constructors
        public Interval() { }
        public Interval(bool isEditable)
        {
            this._IsEditable = isEditable;
        }
        public Interval(TValue begin, TValue end)
        {
            this.BeginValue = begin;
            this.EndValue = end;
        }
        public Interval(TValue begin, TValue end, bool isEditable)
        {
            this.BeginValue = begin;
            this.EndValue = end;
            this._IsEditable = isEditable;
        }
        public override string ToString()
        {
            return "<" + (this.HasBegin ? this.Begin.ToString() : "...") + " ÷ " + (this.HasEnd ? this.End.ToString() : "...") + ">";
        }
        #endregion
        #region Public data properties
        /// <summary>
        /// Begin value
        /// </summary>
        public TValue Begin { get { return _Begin; } set { if (this._IsEditable) { _Begin = value; _HasBegin = true; } } } private TValue _Begin;
        /// <summary>
        /// Begin value, can be set a new value (even to not Editable instance)
        /// </summary>
        protected TValue BeginValue { get { return _Begin; } set { _Begin = value; _HasBegin = true; } }
        /// <summary>
        /// true when Begin value is filled
        /// </summary>
        public bool HasBegin { get { return _HasBegin; } } private bool _HasBegin;
        /// <summary>
        /// End value
        /// </summary>
        public TValue End { get { return _End; } set { if (this._IsEditable) { _End = value; _HasEnd = true; } } } private TValue _End;
        /// <summary>
        /// End value, can be set a new value (even to not Editable instance)
        /// </summary>
        protected TValue EndValue { get { return _End; } set { _End = value; _HasEnd = true; } }
        /// <summary>
        /// true when End value is filled
        /// </summary>
        public bool HasEnd { get { return _HasEnd; } } private bool _HasEnd;
        /// <summary>
        /// true when Begin and End has value, and End is greater than Begin.
        /// When an interval has (End == Begin), then this interval has no effect for other interval (has Zero-Size, and can not Add or Sub any real value to other intervals).
        /// </summary>
        public bool IsFilled { get { return _HasBegin && _HasEnd && IsLower(_Begin, _End); } }
        /// <summary>
        /// true when this interval can set new values into Begin and End
        /// </summary>
        public bool IsEditable { get { return _IsEditable; } } private bool _IsEditable;

        /// <summary>
        /// Clone of values in this instance.
        /// </summary>
        public Interval<TValue> ValueClone
        {
            get
            {
                Interval<TValue> clone = (Interval<TValue>)this.MemberwiseClone();
                return clone;
            }
        }
        #endregion
        #region Detector (Contains, CanMergeWith, IsWholeBefore, IsWholeAfter)
        /// <summary>
        /// Returns true when parameter item is whole contained in this, not any part of item is outside of this.
        /// Examples:
        /// Contains = true for: this = { 10; 20 }, item = { 12; 18 }; or this = { 10; 20 }, item = { 12; 20 }; or this = { 10; 20 }, item = { 10; 20 };
        /// Contains = false for: this = { 10; 20 }, item = { 5; 8 }; or this = { 10; 20 }, item = { 5; 10 }; or this = { 10; 20 }, item = { 5; 30 };
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal bool Contains(Interval<TValue> item)
        {
            IntervalRelation relation = this.GetRelationFor(item);
            return Contains(relation);
        }
        /// <summary>
        /// Returns true when parameter item can be merged into this interval = intervals has some common area, or are seamlessly contuinuous.
        /// Examples:
        /// CanMergeWith = true for: this = { 10; 20 }, item = { 5; 18 }; or this = { 10; 20 }, item = { 5; 10 }; or this = { 10; 20 }, item = { 5; 30 };
        /// CanMergeWith = false for: this = { 10; 20 }, item = { 5; 8 }; or this = { 10; 20 }, item = { 25; 30 };
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal bool CanMergeWith(Interval<TValue> item)
        {
            IntervalRelation relation = this.GetRelationFor(item);
            return CanMergeWith(relation);
        }
        /// <summary>
        /// Returns true when parameter item is whole before this interval = intervals has not common area, interval are not continuously (can not merge).
        /// Examples:
        /// IsWholeBefore = true for: this = { 10; 20 }, item = { 5; 8 };
        /// IsWholeBefore = false for: this = { 10; 20 }, item = { 5; 10 }; or this = { 10; 20 }, item = { 10; 30 };
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal bool IsWholeBefore(Interval<TValue> item)
        {
            IntervalRelation relation = this.GetRelationFor(item);
            return IsWholeBefore(relation);
        }
        /// <summary>
        /// Returns true when parameter item is whole after this interval = intervals has not common area, interval are not continuously (can not merge).
        /// Examples:
        /// IsWholeAfter = true for: this = { 10; 20 }, item = { 21; 25 };
        /// IsWholeAfter = false for: this = { 10; 20 }, item = { 5; 10 }; or this = { 10; 20 }, item = { 20; 30 };
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal bool IsWholeAfter(Interval<TValue> item)
        {
            IntervalRelation relation = this.GetRelationFor(item);
            return IsWholeAfter(relation);
        }
        #endregion
        #region MergeWith
        /// <summary>
        /// Merge values (Begin and End) from parameter item into this values.
        /// This is: this item will contain Begin = Min(Begins); End = Max(Ends).  End.
        /// </summary>
        /// <param name="item"></param>
        public void MergeWith(Interval<TValue> item)
        {
            this.MergeWith(item, null);
        }
        /// <summary>
        /// Merge values (Begin and End) from parameter item into this values.
        /// This is: this item will contain Begin = Min(Begins); End = Max(End
        /// </summary>
        /// <param name="item"></param>
        internal void MergeWith(Interval<TValue> item, IntervalRelation? relation)
        {
            if (item == null || !item.IsFilled) return;
            if (!this.IsFilled)
            {
                this.BeginValue = item.Begin;
                this.EndValue = item.End;
            }
            else
            {
                if (!relation.HasValue) relation = this.GetRelationFor(item);
                if (relation.Value.HasFlag(IntervalRelation.BeginBeforeBegin)) this.BeginValue = item.Begin;
                if (relation.Value.HasFlag(IntervalRelation.EndAfterEnd)) this.EndValue = item.End;
            }
        }
        #endregion
        #region <T> Comparators
        protected static bool IsLower(TValue a, TValue b) { return (a.CompareTo(b) < 0); }
        protected static bool IsEqual(TValue a, TValue b) { return (a.CompareTo(b) == 0); }
        protected static bool IsGreater(TValue a, TValue b) { return (a.CompareTo(b) >= 0); }
        protected static bool IsLowerOrEqual(TValue a, TValue b) { return (a.CompareTo(b) <= 0); }
        protected static bool IsGreaterOrEqual(TValue a, TValue b) { return (a.CompareTo(b) >= 0); }
        protected static bool IsEqual(Interval<TValue> a, Interval<TValue> b)
        {
            return (a.IsFilled && b.IsFilled && IsEqual(a.Begin, b.Begin) && IsEqual(a.End, b.End));
        }
        /// <summary>
        /// Returns relation of other interval to this interval.
        /// When this is not filled (IsFilled = false) or other interval is null or is not filled, then return None.
        /// This is, when any interval has Begin == End, then return is None.
        /// Other values:
        /// when this = { 10; 20 } and item is { 5; 8 }, then return is BeginBeforeBegin | EndBeforeBegin;
        /// when this = { 10; 20 } and item is { 5; 10 }, then return is BeginBeforeBegin | EndOnBegin;
        /// when this = { 10; 20 } and item is { 5; 15 }, then return is BeginBeforeBegin | EndInner;
        /// when this = { 10; 20 } and item is { 5; 20 }, then return is BeginBeforeBegin | EndOnEnd;
        /// when this = { 10; 20 } and item is { 5; 25 }, then return is BeginBeforeBegin | EndAfterEnd;
        /// when this = { 10; 20 } and item is { 10; 15 }, then return is BeginOnBegin | EndInner;
        /// when this = { 10; 20 } and item is { 10; 20 }, then return is BeginOnBegin | EndOnEnd;
        /// when this = { 10; 20 } and item is { 10; 25 }, then return is BeginOnBegin | EndAfterEnd;
        /// when this = { 10; 20 } and item is { 15; 18 }, then return is BeginInner | EndInner;
        /// when this = { 10; 20 } and item is { 15; 20 }, then return is BeginInner | EndOnEnd;
        /// when this = { 10; 20 } and item is { 15; 25 }, then return is BeginInner | EndAfterEnd;
        /// when this = { 10; 20 } and item is { 20; 25 }, then return is BeginOnEnd | EndAfterEnd;
        /// when this = { 10; 20 } and item is { 25; 30 }, then return is BeginAfterEnd | EndAfterEnd;
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public IntervalRelation GetRelationFor(Interval<TValue> item)
        {
            if (!this.IsFilled) return IntervalRelation.None;
            if (item == null || !item.IsFilled) return IntervalRelation.None;

            IntervalRelation beginRelation = GetRelationFor(this.Begin, this.End, item.Begin);
            IntervalRelation endRelation = GetRelationFor(this.Begin, this.End, item.End);
            return (IntervalRelation)((((int)beginRelation) << 8) | (((int)endRelation) << 16));
        }
        /// <summary>
        /// Returns relation of point to this interval.
        /// For example:
        /// when this = { 10; 20 } and point is 5, then return is PointBeforeBegin;
        /// when this = { 10; 20 } and point is 10, then return is PointOnBegin;
        /// when this = { 10; 20 } and point is 15, then return is PointInner;
        /// when this = { 10; 20 } and point is 20, then return is PointOnEnd;
        /// when this = { 10; 20 } and point is 25, then return is PointAfterEnd;
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public IntervalRelation GetRelationFor(TValue point)
        {
            if (!this.IsFilled) return IntervalRelation.None;
            return GetRelationFor(this.Begin, this.End, point);
        }
        #endregion
        #region IMergeable, IValueCloneable members
        void IMergeable.MergeWith(IMergeable other) { this.MergeWith(other as Interval<TValue>); }
        object IValueCloneable.ValueClone { get { return this.ValueClone; } }
        #endregion
    }
    /// <summary>
    /// Base class for intervals, support for compare, interval, merge and so on
    /// </summary>
    public class IntervalBase
    {
        /// <summary>
        /// Returns relation of point to specified interval (begin ÷ end).
        /// For example:
        /// when interval = { 10; 20 } and point is 5, then return is PointBeforeBegin;
        /// when interval = { 10; 20 } and point is 10, then return is PointOnBegin;
        /// when interval = { 10; 20 } and point is 15, then return is PointInner;
        /// when interval = { 10; 20 } and point is 20, then return is PointOnEnd;
        /// when interval = { 10; 20 } and point is 25, then return is PointAfterEnd;
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static IntervalRelation GetRelationFor(IComparable begin, IComparable end, IComparable point)
        {
            int cbe = begin.CompareTo(end);
            if (cbe >= 0) return IntervalRelation.None;

            int cb = -(begin.CompareTo(point));                      // -1: point is before Begin;  0: point is == Begin;  +1: point is after Begin
            if (cb < 0) return IntervalRelation.PointBeforeBegin;
            if (cb == 0) return IntervalRelation.PointOnBegin;

            int ce = -(end.CompareTo(point));                        // -1: point is before End;    0: point is == End;    +1: point is after End
            if (cb > 0 && ce < 0) return IntervalRelation.PointInner;
            if (ce == 0) return IntervalRelation.PointOnEnd;
            if (ce > 0) return IntervalRelation.PointAfterEnd;

            return IntervalRelation.None;
        }
        /// <summary>
        /// Returns true when relation (parameter) descript state, where tested item is whole contained in root item (not any part of tested item is outside of root item).
        /// Examples:
        /// Contains = true for: this = { 10; 20 }, item = { 12; 18 }; or this = { 10; 20 }, item = { 12; 20 }; or this = { 10; 20 }, item = { 10; 20 };
        /// Contains = false for: this = { 10; 20 }, item = { 5; 8 }; or this = { 10; 20 }, item = { 5; 10 }; or this = { 10; 20 }, item = { 5; 30 };
        /// </summary>
        /// <param name="relation">relation from base to tested interval. Can be obtained as: rootItem.GetRelationFor(testItem);</param>
        /// <returns></returns>
        public static bool Contains(IntervalRelation relation)
        {
            bool hasAnyOutside = (relation == IntervalRelation.None || ((relation & (IntervalRelation.BeginBeforeBegin | IntervalRelation.EndAfterEnd)) != 0));
            return !hasAnyOutside;
        }
        /// <summary>
        /// Returns true when relation (parameter) descript state, where tested item is whole or partially over root item (tested item can has any part outside root item, but between test and root are not empty distance).
        /// Items can be merged without problem.
        /// Examples:
        /// CanMergeWith = true for: this = { 10; 20 }, item = { 5; 18 }; or this = { 10; 20 }, item = { 5; 10 }; or this = { 10; 20 }, item = { 5; 30 };
        /// CanMergeWith = false for: this = { 10; 20 }, item = { 5; 8 }; or this = { 10; 20 }, item = { 25; 30 };
        /// </summary>
        /// <param name="relation">relation from base to tested interval. Can be obtained as: rootItem.GetRelationFor(testItem);</param>
        /// <returns></returns>
        public static bool CanMergeWith(IntervalRelation relation)
        {
            bool isEmptySpaceBetween = (relation == IntervalRelation.None || ((relation & (IntervalRelation.EndBeforeBegin | IntervalRelation.BeginAfterEnd)) != 0));
            return !isEmptySpaceBetween;
        }
        /// <summary>
        /// Returns true when relation (parameter) descript state, where tested item is whole before root item = intervals has not common area, interval are not continuously (can not merge).
        /// Examples:
        /// IsWholeBefore = true for: this = { 10; 20 }, item = { 5; 8 };
        /// IsWholeBefore = false for: this = { 10; 20 }, item = { 5; 10 }; or this = { 10; 20 }, item = { 10; 30 };
        /// </summary>
        /// <param name="relation">relation from base to tested interval. Can be obtained as: rootItem.GetRelationFor(testItem);</param>
        /// <returns></returns>
        public static bool IsWholeBefore(IntervalRelation relation)
        {
            bool isBefore = (relation != IntervalRelation.None && ((relation & (IntervalRelation.EndBeforeBegin)) != 0));
            return isBefore;
        }
        /// <summary>
        /// Returns true when relation (parameter) descript state, where tested item is whole after root item = intervals has not common area, interval are not continuously (can not merge).
        /// Examples:
        /// IsWholeAfter = true for: this = { 10; 20 }, item = { 21; 25 };
        /// IsWholeAfter = false for: this = { 10; 20 }, item = { 5; 10 }; or this = { 10; 20 }, item = { 20; 30 };
        /// </summary>
        /// <param name="relation">relation from base to tested interval. Can be obtained as: rootItem.GetRelationFor(testItem);</param>
        /// <returns></returns>
        public static bool IsWholeAfter(IntervalRelation relation)
        {
            bool isAfter = (relation != IntervalRelation.None && ((relation & (IntervalRelation.BeginAfterEnd)) != 0));
            return isAfter;
        }
    }
    #endregion
    #region class PointArray : chain (of PrevNextItem) with items with one Point (as point on axis) and an Value for this point
    /// <summary>
    /// PointArray : chain (of PrevNextItem) with items with one Point (as point on axis) and an Value for this point
    /// </summary>
    /// <typeparam name="TPoint">Type of Point on axis (numeric, date)</typeparam>
    /// <typeparam name="TValue">Type of Value for this TPoint</typeparam>
    public class PointArray<TPoint, TValue>
        where TPoint : IComparable<TPoint>, IComparable
        where TValue : class, IDeepCloneable, new()
    {
        #region Constructor, First item
        public PointArray()
        { }
        public PointArray(TPoint point)
        {
            this.First = new PrevNextItem<PointItem>(new PointItem(point));
        }
        public PointArray(TPoint point, TValue value)
        {
            this.First = new PrevNextItem<PointItem>(new PointItem(point, value));
        }
        protected PrevNextItem<PointItem> First;
        /// <summary>
        /// Array of all items in this PointArray (or null, when this.First is null).
        /// </summary>
        public List<PointItem> Items { get { return (this.First != null ? this.First.Items : null); } }
        /// <summary>
        /// Array of all items in this PointArray (or null, when this.First is null).
        /// </summary>
        public List<PrevNextItem<PointItem>> PrevNextItems { get { return (this.First != null ? this.First.PrevNextItems : null); } }
        #endregion
        #region Clear()
        /// <summary>
        /// Clear this array.
        /// </summary>
        public void Clear()
        {
            if (this.First != null)
                this.First.PrevNextItems.ForEach(i => i.UnLink());
            this.First = null;
        }
        #endregion
        #region Search() for sub-array for range of Point
        /// <summary>
        /// Search and returns array of sequence items, which contains all item for specified range.
        /// When this instance does not contain 
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="insertPoints"></param>
        /// <returns></returns>
        public List<PrevNextItem<PointItem>> Search(TPoint begin, TPoint end, bool insertPoints)
        {
            // Search item: for last before, for all inner interval, and for first after:
            PrevNextItem<PointItem> itemBefore = null;
            bool haveBegin = false;
            List<PrevNextItem<PointItem>> result = new List<PrevNextItem<PointItem>>();
            bool haveEnd = false;
            PrevNextItem<PointItem> itemAfter = null;

            List<PrevNextItem<PointItem>> items = this.PrevNextItems;
            if (items != null)
            {
                int count = items.Count;
                for (int i = 0; i < count; i++)
                {
                    PrevNextItem<PointItem> item = items[i];
                    var relation = IntervalBase.GetRelationFor(begin, end, item.Value.Point);
                    switch (relation)
                    {
                        case IntervalRelation.PointBeforeBegin:
                            itemBefore = item;
                            break;
                        case IntervalRelation.PointOnBegin:
                            haveBegin = true;
                            result.Add(item);
                            break;
                        case IntervalRelation.PointInner:
                            result.Add(item);
                            break;
                        case IntervalRelation.PointOnEnd:
                            haveEnd = true;
                            result.Add(item);
                            break;
                        case IntervalRelation.PointAfterEnd:
                            itemAfter = item;
                            break;
                    }
                    if (itemAfter != null)
                        break;
                }
            }

            if (!insertPoints)
            {   // Do not insert exact point for begin and end, only as first item return item before begin (if exists):
                if (!haveBegin && itemBefore != null)
                    result.Insert(0, itemBefore);
            }
            else
            {   // When do not exists items with exact Point (begin, end), then insert new items for these points:
                if (!haveBegin)
                {   // New PrevNextItem for exact (begin) Point:
                    TValue value = (itemBefore != null ? itemBefore.Value.Value.DeepClone as TValue : new TValue());
                    PointItem beginItem = new PointItem(begin, value);
                    PrevNextItem<PointItem> linkItem = new PrevNextItem<PointItem>(beginItem);

                    // Insert new linkItem (for beginItem) into chain in this instance:
                    if (itemBefore != null)
                    {   // An item with (Point < begin) exists, then insert new item (with Point == begin) after (itemBefore):
                        itemBefore.InsertAfter(linkItem);
                    }
                    else
                    {   // In this array does not exists item with (Point < begin). Create new item as new First item:
                        if (this.First != null)
                            this.First.InsertBefore(linkItem);
                        this.First = linkItem;
                    }

                    result.Insert(0, linkItem);
                }

                if (!haveEnd)
                {   // New PrevNextItem for exact (end) Point:
                    PrevNextItem<PointItem> lastItem = (result.Count > 0 ? result[result.Count - 1] : null);
                    TValue value = (lastItem != null ? lastItem.Value.Value.DeepClone as TValue : new TValue());
                    PointItem endItem = new PointItem(end, value);
                    PrevNextItem<PointItem> linkItem = new PrevNextItem<PointItem>(endItem);

                    // Insert new linkItem (for endItem) into chain in this instance:
                    if (lastItem != null)
                    {
                        lastItem.InsertAfter(endItem);
                    }

                    result.Add(linkItem);
                }
            }

            return result;
        }
        #endregion
        #region class PointItem : class with Point and Value
        /// <summary>
        /// PointItem : class with Point and Value
        /// </summary>
        public class PointItem : IValueCloneable
        {
            public PointItem(TPoint point)
            {
                this._Point = point;
                this._Value = default(TValue);
            }
            public PointItem(TPoint point, TValue value)
            {
                this._Point = point;
                this._Value = value;
            }
            public override string ToString()
            {
                return "Point: " + this.Point.ToString() + "; Value: " + this.Value.ToString();
            }
            /// <summary>
            /// Point on any axis
            /// </summary>
            public TPoint Point { get { return _Point; } } private TPoint _Point;
            /// <summary>
            /// Any value
            /// </summary>
            public TValue Value { get { return _Value; } set { _Value = value; } } private TValue _Value;
            public PointItem ValueClone
            {
                get
                {
                    PointItem clone = new PointItem(this.Point);
                    clone.Value = this.Value.DeepClone as TValue;
                    return clone;
                }
            }

            object IValueCloneable.ValueClone { get { return this.ValueClone; } }
        }
        #endregion
        #region <TPoint> Compare
        protected int Compare(TPoint a, TPoint b)
        {
            return a.CompareTo(b);
        }
        #endregion
    }
    #endregion
    #region class PrevNextItem : class with one Value, and Prev and Next reference
    /// <summary>
    /// PrevNextItem : class with one Value, and Prev and Next reference to neighborough items of same type
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public class PrevNextItem<TItem> : IDeepCloneable
        where TItem : class, IValueCloneable
    {
        #region Constructos, Property: <T> Value
        public PrevNextItem() { }
        public PrevNextItem(TItem value) { this.Value = value; }
        public override string ToString()
        {
            return (this.Value != null ? this.Value.ToString() : "NULL");
        }
        public TItem Value { get; set; }
        #endregion
        #region Properties Prev, Next, ValueClone
        /// <summary>
        /// true when this item has not any Prev item (this is First item in chain)
        /// </summary>
        public bool IsFirst { get { return (_Prev == null); } }
        /// <summary>
        /// true when this item has any Prev item (=is not First)
        /// </summary>
        public bool HasPrev { get { return (_Prev != null); } }
        /// <summary>
        /// Previous item. Can be null.
        /// </summary>
        public PrevNextItem<TItem> Prev { get { return _Prev; } } private PrevNextItem<TItem> _Prev;
        /// <summary>
        /// true when this item has any Next item (=is not Last)
        /// </summary>
        public bool HasNext { get { return (_Next != null); } }
        /// <summary>
        /// true when this item has not any Next item (this is Last item in chain)
        /// </summary>
        public bool IsLast { get { return (_Next == null); } }
        /// <summary>
        /// Next item. Can be null.
        /// </summary>
        public PrevNextItem<TItem> Next { get { return _Next; } } private PrevNextItem<TItem> _Next;
        /// <summary>
        /// ValueClone of PrevNextItem contain ValueClone of Value, and null in Prev and Next relation.
        /// </summary>
        public PrevNextItem<TItem> ValueClone
        {
            get
            {
                PrevNextItem<TItem> clone = new PrevNextItem<TItem>();
                clone.Value = this.Value.ValueClone as TItem;
                return clone;
            }
        }
        #endregion
        #region Properties First, Last
        /// <summary>
        /// First item in chain of this.Prev.Prev...
        /// This property never contains null. When this item is first, then First property contain this item.
        /// </summary>
        public PrevNextItem<TItem> First
        {
            get
            {
                PrevNextItem<TItem> first = this;
                while (first.HasPrev)
                {
                    first = first.Prev;
                    if (Object.ReferenceEquals(first, this))
                    {   // Cycle (any of Prev item is equal to this item): we will break this cycle:
                        first._Prev = null;
                        break;
                    }
                }
                return first;
            }
        }
        /// <summary>
        /// Last item in chain of this.Next.Next...
        /// This property never contains null. When this item is last, then Last property contain this item.
        /// </summary>
        public PrevNextItem<TItem> Last
        {
            get
            {
                PrevNextItem<TItem> last = this;
                while (last.HasNext)
                {
                    last = last.Next;
                    if (Object.ReferenceEquals(last, this))
                    {   // Cycle (any of Next item is equal to this item): we will break this cycle:
                        last._Next = null;
                        break;
                    }
                }
                return last;
            }
        }
        #endregion
        #region IDeepCloneable
        public PrevNextItem<TItem> DeepClone
        {
            get
            {
                PrevNextItem<TItem> result = null;         // here will be Clone of this item, this object will be returned as DeepClone.

                PrevNextItem<TItem> item = this.First;
                PrevNextItem<TItem> itemClone = item.ValueClone;
                while (true)
                {
                    if (Object.ReferenceEquals(item, this))
                        result = itemClone;                // Now we have cloned this instance to clone chain: result of this method will be clone of this item.

                    if (!item.HasNext) break;

                    PrevNextItem<TItem> next = item.Next;
                    PrevNextItem<TItem> nextClone = next.ValueClone;
                    Link(itemClone, nextClone);

                    item = next;
                    itemClone = nextClone;
                }

                return result;
            }
        }
        object IDeepCloneable.DeepClone { get { return this.DeepClone; } }
        #endregion
        #region MergeWith, MergeWithPrev, MergeWithNext
        /// <summary>
        /// Merge values (Begin and End) from parameter item into this values.
        /// Does not link any Next nor Prev from item into this! Merge only Begin, End.
        /// </summary>
        /// <param name="item"></param>
        public void MergeWith(TItem other)
        {
            if (this.Value != null && this.Value is IMergeable && other != null && other is IMergeable)
                ((IMergeable)this.Value).MergeWith(other as IMergeable);
        }
        /// <summary>
        /// Merge values (Begin and End) from parameter item into this values.
        /// Does not link any Next nor Prev from item into this! Merge only Begin, End.
        /// </summary>
        /// <param name="item"></param>
        public void MergeWith(PrevNextItem<TItem> other)
        {
            if (this.Value != null && this.Value is IMergeable && other != null && other.Value is IMergeable)
                ((IMergeable)this.Value).MergeWith(other.Value as IMergeable);
        }
        /// <summary>
        /// Merge this.Prev into this instance, this.Prev instance will be dropped, and link this.Prev.Prev to this mutually.
        /// When Value is IMergable, then merge value from Prev into this.Value.
        /// </summary>
        internal void MergeWithPrev()
        {
            if (!this.HasPrev) return;
            PrevNextItem<TItem> prev = this.Prev;
            this.MergeWith(prev);
            Link(prev.Prev, this);
            prev.UnLink();
        }
        /// <summary>
        /// Merge this.Next into this instance, this.Next instance will be dropped, and link this.Next.Next to this mutually
        /// When Value is IMergable, then merge value from Next into this.Value.
        /// </summary>
        internal void MergeWithNext()
        {
            if (!this.HasNext) return;
            PrevNextItem<TItem> next = this.Next;
            this.MergeWith(next);
            Link(this, next.Next);
            next.UnLink();
        }
        #endregion
        #region Link, Unlink, InsertBefore, InsertAfter
        /// <summary>
        /// Unlink from this item items Prev and Next.
        /// </summary>
        public void UnLink()
        {
            this._Prev = null;
            this._Next = null;
        }
        /// <summary>
        /// Link mutually two Items, as { Prev - Next } pair.
        /// One (or both) items can be null.
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="next"></param>
        public static void Link(PrevNextItem<TItem> prev, PrevNextItem<TItem> next)
        {
            if (prev != null) prev._Next = next;
            if (next != null) next._Prev = prev;
        }
        /// <summary>
        /// Insert an new item before this.
        /// Old Prev item will be Prev for newly inserted item.
        /// All items will be correctly linked.
        /// </summary>
        /// <param name="item"></param>
        internal void InsertBefore(TItem item)
        {
            if (item == null) return;
            this._InsertBefore(new PrevNextItem<TItem>(item));
        }
        /// <summary>
        /// Insert an new item before this.
        /// Old Prev item will be Prev for newly inserted item.
        /// All items will be correctly linked.
        /// </summary>
        /// <param name="item"></param>
        internal void InsertBefore(PrevNextItem<TItem> item)
        {
            if (item == null) return;
            this._InsertBefore(item);
        }
        /// <summary>
        /// Insert an new item after this.
        /// Old Next item will be Next for newly inserted item.
        /// All items will be correctly linked.
        /// </summary>
        /// <param name="item"></param>
        internal void InsertAfter(TItem item)
        {
            if (item == null) return;
            this._InsertAfter(new PrevNextItem<TItem>(item));
        }
        /// <summary>
        /// Insert an new item after this.
        /// Old Next item will be Next for newly inserted item.
        /// All items will be correctly linked.
        /// </summary>
        /// <param name="item"></param>
        internal void InsertAfter(PrevNextItem<TItem> item)
        {
            if (item == null) return;
            this._InsertAfter(item);
        }
        private void _InsertBefore(PrevNextItem<TItem> item)
        {
            if (item == null) return;
            item.UnLink();
            PrevNextItem<TItem> oldPrev = this._Prev;
            this._Prev = item;
            Link(oldPrev, this._Prev);
            Link(this._Prev, this);
        }
        private void _InsertAfter(PrevNextItem<TItem> item)
        {
            if (item == null) return;
            item.UnLink();
            PrevNextItem<TItem> oldNext = this._Next;
            this._Next = item;
            Link(this, this._Next);
            Link(this._Next, oldNext);
        }
        #endregion
        #region GetList, GetArray
        /// <summary>
        /// Array of all TItem items, from this.First.Value to Last
        /// </summary>
        public List<TItem> Items
        {
            get 
            {
                List<TItem> result = new List<TItem>();

                PrevNextItem<TItem> item = this.First;
                while (item != null)
                {
                    if (item.Value != null)
                        result.Add(item.Value);
                    item = item.Next;
                }

                return result;
            }
        }
        /// <summary>
        /// Array of all PrevNextItem items, from this.First to Last
        /// </summary>
        public List<PrevNextItem<TItem>> PrevNextItems
        {
            get 
            {
                List<PrevNextItem<TItem>> result = new List<PrevNextItem<TItem>>();

                PrevNextItem<TItem> item = this.First;
                while (item != null)
                {
                    if (item.Value != null)
                        result.Add(item);
                    item = item.Next;
                }

                return result;
            }
        }

        #endregion
    }
    #endregion
    #region enum IntervalRelation
    /// <summary>
    /// Relation 
    /// </summary>
    [Flags]
    public enum IntervalRelation : int
    {
        /// <summary>
        /// Not relation: one or booth interval are null or not filled (IsFilled = false) = has not Begin or End, or End is Lower or Equal (!) to Begin.
        /// When an interval has End == Begin, this interval has not value (has Zero size and does not affect any operation).
        /// </summary>
        None = 0,

        /// <summary>Tested point is before base interval.Begin</summary>
        PointBeforeBegin = 0x0001,
        /// <summary>Tested point is equal to base interval.Begin</summary>
        PointOnBegin = 0x0002,
        /// <summary>Tested point is after base interval.Begin, and before base interval.End</summary>
        PointInner = 0x0004,
        /// <summary>Tested point is equal to base interval.End</summary>
        PointOnEnd = 0x0008,
        /// <summary>Tested point is after base interval.End</summary>
        PointAfterEnd = 0x0010,

        /// <summary>Tested interval has Begin before base interval.Begin</summary>
        BeginBeforeBegin = 0x000100,
        /// <summary>Tested interval has Begin equal to base interval.Begin</summary>
        BeginOnBegin = 0x000200,
        /// <summary>Tested interval has Begin after base interval.Begin, and before base interval.End</summary>
        BeginInner = 0x000400,
        /// <summary>Tested interval has Begin equal to base interval.End</summary>
        BeginOnEnd = 0x000800,
        /// <summary>Tested interval has Begin after base interval.End</summary>
        BeginAfterEnd = 0x001000,

        /// <summary>Tested interval has End before base interval.Begin</summary>
        EndBeforeBegin = 0x010000,
        /// <summary>Tested interval has End equal to base interval.Begin</summary>
        EndOnBegin = 0x020000,
        /// <summary>Tested interval has End after base interval.Begin, and before base interval.End</summary>
        EndInner = 0x040000,
        /// <summary>Tested interval has End equal to base interval.End</summary>
        EndOnEnd = 0x080000,
        /// <summary>Tested interval has End after base interval.End</summary>
        EndAfterEnd = 0x100000
    }
    #endregion
    #region interface IMergeable, IValueCloneable, IDeepCloneable
    public interface IMergeable
    {
        /// <summary>
        /// Merge into this instance all values from parameter
        /// </summary>
        /// <param name="other"></param>
        void MergeWith(IMergeable other);
    }
    public interface IValueCloneable
    {
        /// <summary>
        /// Get a value (=shallow) clone of this instance.
        /// </summary>
        object ValueClone { get; }
    }
    public interface IDeepCloneable
    {
        /// <summary>
        /// Get a deep (=full) clone of this instance.
        /// </summary>
        object DeepClone { get; }
    }
    #endregion
}
