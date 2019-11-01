using System.Collections.Generic;

namespace MiniUDP
{
    internal interface INetPoolable<T>
    where T : INetPoolable<T>
    {
        void Reset();
    }

    internal interface INetPool<T>
    {
        T Allocate();
        void Deallocate(T obj);
    }

    internal class NetPool<T> : INetPool<T>
      where T : INetPoolable<T>, new()
    {
        private readonly Stack<T> freeList;

        public NetPool()
        {
            freeList = new Stack<T>();
        }

        public T Allocate()
        {
            lock (freeList)
            {
                if (freeList.Count > 0)
                {
                    return freeList.Pop();
                }
            }

            T obj = new T();
            obj.Reset();
            return obj;
        }

        public void Deallocate(T obj)
        {
            obj.Reset();
            lock (freeList)
            {
                freeList.Push(obj);
            }
        }
    }
}
