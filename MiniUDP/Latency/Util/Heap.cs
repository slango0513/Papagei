#if DEBUG
using System;
using System.Collections.Generic;

namespace MiniUDP.Util
{
    internal class Heap<T>
    {
        private const int INITIAL_CAPACITY = 0;
        private const int GROW_FACTOR = 2;
        private const int MIN_GROW = 1;

        private int capacity = INITIAL_CAPACITY;
        private T[] heap = new T[INITIAL_CAPACITY];
        private int tail = 0;

        public int Count => tail;
        public int Capacity => capacity;

        protected Comparer<T> Comparer { get; private set; }

        public Heap()
        {
            Comparer = Comparer<T>.Default;
        }

        public Heap(Comparer<T> comparer)
        {
            Comparer = comparer ?? throw new ArgumentNullException("comparer");
        }

        public void Clear()
        {
            tail = 0;
        }

        public void Add(T item)
        {
            if (Count == Capacity)
            {
                Grow();
            }

            heap[tail++] = item;
            BubbleUp(tail - 1);
        }

        public T GetMin()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Heap is empty");
            }

            return heap[0];
        }

        public T ExtractDominating()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Heap is empty");
            }

            T ret = heap[0];
            tail--;
            Swap(tail, 0);
            BubbleDown(0);
            return ret;
        }

        protected bool Dominates(T x, T y)
        {
            return Comparer.Compare(x, y) <= 0;
        }

        private void BubbleUp(int i)
        {
            if (i == 0)
            {
                return;
            }

            if (Dominates(heap[Parent(i)], heap[i]))
            {
                return; // Correct domination (or root)
            }

            Swap(i, Parent(i));
            BubbleUp(Parent(i));
        }

        private void BubbleDown(int i)
        {
            int dominatingNode = Dominating(i);
            if (dominatingNode == i)
            {
                return;
            }

            Swap(i, dominatingNode);
            BubbleDown(dominatingNode);
        }

        private int Dominating(int i)
        {
            int dominatingNode = i;
            dominatingNode =
              GetDominating(YoungChild(i), dominatingNode);
            dominatingNode =
              GetDominating(OldChild(i), dominatingNode);
            return dominatingNode;
        }

        private int GetDominating(int newNode, int dominatingNode)
        {
            if (newNode >= tail)
            {
                return dominatingNode;
            }

            if (Dominates(heap[dominatingNode], heap[newNode]))
            {
                return dominatingNode;
            }

            return newNode;
        }

        private void Swap(int i, int j)
        {
            T tmp = heap[i];
            heap[i] = heap[j];
            heap[j] = tmp;
        }

        private static int Parent(int i)
        {
            return (i + 1) / 2 - 1;
        }

        private static int YoungChild(int i)
        {
            return (i + 1) * 2 - 1;
        }

        private static int OldChild(int i)
        {
            return YoungChild(i) + 1;
        }

        private void Grow()
        {
            int newCapacity =
              capacity * GROW_FACTOR + MIN_GROW;
            T[] newHeap = new T[newCapacity];
            Array.Copy(heap, newHeap, capacity);
            heap = newHeap;
            capacity = newCapacity;
        }
    }
}
#endif