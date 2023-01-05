using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Games.Sudoku.Data
{
    public class CycleBuffer<T>
    {
        /// <summary>
        /// Konstruktor pro danou kapacitu
        /// </summary>
        /// <param name="capacity"></param>
        public CycleBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentException("'CycleBuffer' akceptuje jako kapacitu pouze kladné číslo.");
            if (capacity > MaxLength) throw new ArgumentException($"'CycleBuffer' neakceptuje počet prvků {capacity}, maximum je {MaxLength }.");
            __Array = new T[capacity];
            __Capacity = 0;
            _Clear(false);
        }
        /// <summary>
        /// Maximální počet prvků
        /// </summary>
        public static int MaxLength { get { return 1000000; } }
        private readonly T[] __Array;
        private readonly int __Capacity;
        private int __Pointer;
        private int __Last;
        /// <summary>
        /// Přidá daný prvek, bude na pozici [0]
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            lock(__Array)
            {
                var pointer = __Pointer + 1;

                if (pointer >= __Capacity) 
                    pointer = 0;
                else if (pointer > __Last) 
                    __Last = pointer;

                __Pointer = pointer;
                __Array[pointer] = item;
            }
        }
        /// <summary>
        /// Vyprázdní buffer
        /// </summary>
        public void Clear()
        {
            _Clear(true);
        }
        /// <summary>
        /// Vyprázdní buffer a volitelně naplní default
        /// </summary>
        private void _Clear(bool reset)
        {
            if (reset)
            {
                T empty = default(T);
                for (int i = 0; i < __Last; i++)
                    __Array[i] = empty;
            }

            __Pointer = -1;
            __Last = -1;
        }

        public T this[int index]
        {
            get
            { }
        }
        public T[] Items
        {
            get
            {

            }
        }
    }
}
