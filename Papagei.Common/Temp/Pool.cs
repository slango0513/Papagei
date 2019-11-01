using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei
{
    public interface IPoolable<T> /*where T : IPoolable<T>*/
    {
        IPool<T> Pool { get; set; }
        void Reset();
    }

    public interface IPool<T>
    {
        T Allocate();
        void Deallocate(T obj);
    }

    public abstract class PoolBase<T> : IPool<T> where T : IPoolable<T>
    {
        private readonly Stack<T> freeList = new Stack<T>();

        protected abstract T Create();

        public T Allocate()
        {
            T obj;
            if (freeList.Count > 0)
            {
                obj = freeList.Pop();
            }
            else
            {
                obj = Create();
            }

            obj.Pool = this;
            obj.Reset();
            return obj;
        }

        public void Deallocate(T obj)
        {
            Debug.Assert(obj.Pool == this);

            obj.Reset();
            obj.Pool = null; // Prevent multiple frees
            freeList.Push(obj);
        }
    }

    public class Pool<T> : PoolBase<T> where T : IPoolable<T>, new()
    {
        protected override T Create()
        {
            return new T();
        }
    }

    public class Pool<TBase, TDerived> : PoolBase<TBase> where TBase : IPoolable<TBase> where TDerived : TBase, new()
    {
        protected override TBase Create()
        {
            return new TDerived();
        }
    }
}
