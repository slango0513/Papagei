using System.Collections.Generic;
using System.Threading;

namespace MiniUDP
{
    internal class NetPipeline<T>
    {
        private readonly Queue<T> queue;
        private volatile int count;

        public NetPipeline()
        {
            queue = new Queue<T>();
            count = 0;
        }

        public bool TryDequeue(out T obj)
        {
            // This check can be done out of lock...
            obj = default(T);
            if (count <= 0)
            {
                return false;
            }

            lock (queue)
            {
                obj = queue.Dequeue();
                Interlocked.Decrement(ref count);
                return true;
            }
        }

        public void Enqueue(T obj)
        {
            lock (queue)
            {
                queue.Enqueue(obj);
            }

            // ...as long as this ++ is atomic and happens after we add
            Interlocked.Increment(ref count);
        }
    }
}
