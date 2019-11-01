using System;
using System.Collections.Generic;

namespace Papagei
{
    /// <summary>
    /// Helper function for delaying events and other timed occurences. 
    /// Not used internally.
    /// </summary>
    public class DelayList<T> where T : class, ITimedValue, ILinkListNode<T>
    {
        private readonly LinkList<T> list = new LinkList<T>();

        public int Count => list.Count;
        public T Newest => list.Last;
        public T Oldest => list.First;

        public void Clear(Action<T> cleanup)
        {
            if (cleanup != null)
            {
                list.ForEach(cleanup);
            }

            list.Clear();
        }

        public void ForEach(Action<T> action)
        {
            list.ForEach(action);
        }

        /// <summary>
        /// Inserts a value in the buffer. Allows for duplicate ticks.
        /// </summary>
        public void Insert(T value)
        {
            var iter = list.First;
            if (iter == null)
            {
                list.Add(value);
            }
            else
            {
                while (iter != null)
                {
                    if (iter.Tick >= value.Tick)
                    {
                        list.InsertBefore(iter, value);
                        return;
                    }
                    iter = list.GetNext(iter);
                }
                list.Add(value);
            }
        }

        /// <summary>
        /// Removes all elements older than the given tick.
        /// </summary>
        public IEnumerable<T> Drain(Tick tick)
        {
            while (list.First != null)
            {
                if (list.First.Tick <= tick)
                {
                    yield return list.RemoveFirst();
                }
                else
                {
                    break;
                }
            }
        }
    }
}
