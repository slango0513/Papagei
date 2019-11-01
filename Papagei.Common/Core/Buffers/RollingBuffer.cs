using System;
using System.Collections.Generic;

namespace Papagei
{
    /// <summary>
    /// A rolling buffer that contains a sliding window of the most recent
    /// stored values.
    /// </summary>
    public class RollingBuffer<T>
    {
        public int Count { get; private set; } = 0;

        private int start = 0;

        private readonly T[] data;
        private readonly int capacity;

        public RollingBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }

            this.capacity = capacity;

            data = new T[capacity];
        }

        public void Clear()
        {
            Count = 0;
            start = 0;
        }

        /// <summary>
        /// Stores a value as latest.
        /// </summary>
        public void Store(T value)
        {
            if (Count < capacity)
            {
                data[Count++] = value;
                IncrementStart();
            }
            else
            {
                data[start] = value;
                IncrementStart();
            }
        }

        /// <summary>
        /// Returns all values, but not in order.
        /// </summary>
        public IEnumerable<T> GetValues()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return data[i];
            }
        }

        private void IncrementStart()
        {
            start = (start + 1) % capacity;
        }
    }
}
