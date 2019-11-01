using System.Collections.Generic;

namespace Papagei
{
    /// <summary>
    /// A rolling queue that maintains entries in order. Designed to access
    /// the entry at a given tick, or the most recent entry before it.
    /// </summary>
    public class QueueBuffer<T> where T : class, IPoolable<T>, ITimedValue
    {
        public T Latest { get; private set; } = default;

        private readonly Queue<T> data = new Queue<T>();
        private readonly int capacity;

        public QueueBuffer(int capacity)
        {
            this.capacity = capacity;
        }

        public void Store(T val)
        {
            if (data.Count >= capacity)
            {
                var value = data.Dequeue();
                value.Pool.Deallocate(value);
            }

            data.Enqueue(val);
            Latest = val;
        }

        public T LatestAt(Tick tick)
        {
            // TODO: Binary Search
            T retVal = null;
            foreach (var val in data)
            {
                if (val.Tick <= tick)
                {
                    retVal = val;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Clears the buffer, freeing all contents.
        /// </summary>
        public void Clear()
        {
            foreach (var val in data)
            {
                val.Pool.Deallocate(val);
            }

            data.Clear();
            Latest = null;
        }
    }
}
