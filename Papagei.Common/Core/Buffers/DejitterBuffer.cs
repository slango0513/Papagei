using System.Collections.Generic;

namespace Papagei
{
    /// <summary>
    /// Pre-allocated random access buffer for dejittering values. Preferable to
    /// DejitterList because of fast insertion and lookup, but harder to use.
    /// </summary>
    public class DejitterBuffer<T> where T : class, IPoolable<T>, ITimedValue
    {
        private readonly TickComparer _comparer = new TickComparer();

        private int Compare(T x, T y)
        {
            return _comparer.Compare(x.Tick, y.Tick);
        }

        // Used for converting a key to an index. For example, the server may only
        // send a snapshot every two ticks, so we would divide the tick number
        // key by 2 so as to avoid wasting space in the frame buffer
        private readonly int divisor;

        /// <summary>
        /// The most recent value stored in this buffer.
        /// </summary>
        internal T Latest
        {
            get
            {
                if (latestIdx < 0)
                {
                    return null;
                }

                return data[latestIdx];
            }
        }

        private readonly T[] data;
        private int latestIdx = -1;
        private readonly List<T> returnList = new List<T>(); // A reusable list for returning results

        internal IEnumerable<T> Values
        {
            get
            {
                foreach (var value in data)
                {
                    if (value != null)
                    {
                        yield return value;
                    }
                }
            }
        }

        public DejitterBuffer(int capacity, int divisor = 1)
        {
            this.divisor = divisor;
            data = new T[capacity / divisor];
        }

        /// <summary>
        /// Clears the buffer, freeing all contents.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < data.Length; i++)
            {
                {
                    // SafeReplace
                    if (data[i] != null)
                    {
                        var val = data[i];
                        val.Pool.Deallocate(val);
                    }
                    data[i] = null;
                }
            }

            latestIdx = -1;
        }

        /// <summary>
        /// Stores a value. Will not replace a stored value with an older one.
        /// </summary>
        public bool Store(T value)
        {
            var index = TickToIndex(value.Tick);
            var store = false;

            if (latestIdx < 0)
            {
                store = true;
            }
            else
            {
                var latest = data[latestIdx];
                if (value.Tick >= latest.Tick)
                {
                    store = true;
                }
            }

            if (store)
            {
                {
                    // SafeReplace
                    if (data[index] != null)
                    {
                        var val = data[index];
                        val.Pool.Deallocate(val);
                    }
                    data[index] = value;
                }
                latestIdx = index;
            }
            return store;
        }

        public T Get(Tick tick)
        {
            if (tick == Tick.INVALID)
            {
                return null;
            }

            T result = data[TickToIndex(tick)];
            if ((result != null) && (result.Tick == tick))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Given a tick, returns the the following values:
        /// - The value at or immediately before the tick (current)
        /// - The value immediately after that (next)
        /// 
        /// Runs in O(n).
        /// </summary>
        public void GetFirstAfter(Tick currentTick, out T current, out T next)
        {
            current = null;
            next = null;

            if (currentTick == Tick.INVALID)
            {
                return;
            }

            for (int i = 0; i < data.Length; i++)
            {
                var value = data[i];
                if (value != null)
                {
                    if (value.Tick > currentTick)
                    {
                        if ((next == null) || (value.Tick < next.Tick))
                        {
                            next = value;
                        }
                    }
                    else if ((current == null) || (value.Tick > current.Tick))
                    {
                        current = value;
                    }
                }
            }
        }

        /// <summary>
        /// Finds the latest value at or before a given tick. O(n)
        /// </summary>
        public T GetLatestAt(Tick tick)
        {
            if (tick == Tick.INVALID)
            {
                return null;
            }

            var result = Get(tick);
            if (result != null)
            {
                return result;
            }

            for (int i = 0; i < data.Length; i++)
            {
                var value = data[i];
                if (value != null)
                {
                    if (value.Tick == tick)
                    {
                        return value;
                    }

                    if (value.Tick < tick)
                    {
                        if ((result == null) || (result.Tick < value.Tick))
                        {
                            result = value;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Finds all items at or later than the given tick, in order.
        /// </summary>
        public IList<T> GetRange(Tick start)
        {
            returnList.Clear();
            if (start == Tick.INVALID)
            {
                return returnList;
            }

            for (int i = 0; i < data.Length; i++)
            {
                var val = data[i];
                if ((val != null) && (val.Tick >= start))
                {
                    returnList.Add(val);
                }
            }

            returnList.Sort(Compare);
            return returnList;
        }

        /// <summary>
        /// Finds all items with ticks in the inclusive range [start, end]
        /// and also returns the value immediately following (if one exists)
        /// </summary>
        public IList<T> GetRangeAndNext(Tick start, Tick end, out T next)
        {
            next = null;
            returnList.Clear();
            if (start == Tick.INVALID)
            {
                return returnList;
            }

            var lowest = Tick.INVALID;
            for (int i = 0; i < data.Length; i++)
            {
                var val = data[i];
                if (val != null)
                {
                    if ((val.Tick >= start) && (val.Tick <= end))
                    {
                        returnList.Add(val);
                    }

                    if (val.Tick > end)
                    {
                        if (lowest == Tick.INVALID || val.Tick < lowest)
                        {
                            next = val;
                            lowest = val.Tick;
                        }
                    }
                }
            }

            returnList.Sort(Compare);
            return returnList;
        }

        public bool Contains(Tick tick)
        {
            if (tick == Tick.INVALID)
            {
                return false;
            }

            var result = data[TickToIndex(tick)];
            if ((result != null) && (result.Tick == tick))
            {
                return true;
            }

            return false;
        }

        public bool TryGet(Tick tick, out T value)
        {
            if (tick == Tick.INVALID)
            {
                value = null;
                return false;
            }

            value = Get(tick);
            return value != null;
        }

        private int TickToIndex(Tick tick)
        {
            return (int)(tick.RawValue / divisor) % data.Length;
        }
    }
}
