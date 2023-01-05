using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Games.Sudoku.Data
{
    /// <summary>
    /// Cyklický buffer s danou maximální kapacitou.
    /// <para/>
    /// Cyklický buffer je podoben frontě Queue s exaktně daným maximálním počtem prvků. Po přidání 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CycleBuffer<T> : IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>
    {
        /// <summary>
        /// Konstruktor pro danou kapacitu.
        /// </summary>
        /// <param name="capacity"></param>
        public CycleBuffer(int capacity)
            : base()
        {
            if (capacity <= 0) throw new ArgumentException("'CycleBuffer' akceptuje jako kapacitu pouze kladné číslo.");
            if (capacity > MaxLength) throw new ArgumentException($"'CycleBuffer' neakceptuje počet prvků {capacity}, maximum je {MaxLength }.");
            __Queue = new Queue<T>();
            __Capacity = capacity;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"CycleBuffer<{typeof(T).FullName}>: Capacity: {__Capacity}; Count: {Count}";
        }
        /// <summary>
        /// Maximální počet prvků
        /// </summary>
        public static int MaxLength { get { return 1000000; } }
        private Queue<T> __Queue;
        private readonly int __Capacity;
        /// <summary>
        /// Přidá daný prvek, bude na pozici [0]
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            while (__Queue.Count >= __Capacity)
                __Queue.Dequeue();
            __Queue.Enqueue(item);
        }
        /// <summary>
        /// Počet prvků
        /// </summary>
        public int Count { get { return __Queue.Count; } }
        /// <summary>
        /// Vyprázdní buffer
        /// </summary>
        public void Clear() { __Queue.Clear(); }
        /// <summary>
        /// Prvek na daném indexu
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index] { get { return Items[index]; } }
        /// <summary>
        /// Všechny prvky
        /// </summary>
        public T[] Items { get { return __Queue.ToArray(); } }
        #region Implementace interface
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return ((IEnumerable<T>)this.__Queue).GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable)this.__Queue).GetEnumerator(); }
        void ICollection.CopyTo(Array array, int index) { ((ICollection)this.__Queue).CopyTo(array, index); }
        int ICollection.Count { get { return this.Count; } }
        object ICollection.SyncRoot { get { return ((ICollection)this.__Queue).SyncRoot; } }
        bool ICollection.IsSynchronized { get { return ((ICollection)this.__Queue).IsSynchronized; } }
        int IReadOnlyCollection<T>.Count { get { return this.Count; } }
        #endregion
    }
}
