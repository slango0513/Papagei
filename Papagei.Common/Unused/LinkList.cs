using System;
using System.Collections.Generic;

namespace Papagei
{
    public interface ILinkListNode<T> where T : class, ILinkListNode<T>
    {
        T Next { get; set; }
        T Prev { get; set; }
        LinkList<T> List { get; set; }
    }

    public struct LinkListIterator<T> where T : class, ILinkListNode<T>
    {
        T iter;

        internal LinkListIterator(T first)
        {
            iter = first;
        }

        public bool Next(out T value)
        {
            if (iter != null)
            {
                value = iter;
                iter = iter.Next;
                return true;
            }

            value = null;
            return false;
        }
    }

    public class LinkList<T> where T : class, ILinkListNode<T>
    {
        public int Count { get; private set; } = 0;
        public T First { get; private set; } = default;
        public T Last { get; private set; } = default;

        public LinkListIterator<T> GetIterator()
        {
            return new LinkListIterator<T>(First);
        }

        public LinkListIterator<T> GetIterator(T startAfter)
        {
#if DEBUG
            if (startAfter.List != this)
            {
                throw new AccessViolationException("Node is not in this list");
            }
#endif
            if (startAfter == null)
            {
                throw new AccessViolationException();
            }

            return new LinkListIterator<T>(startAfter.Next);
        }

        /// <summary>
        /// Adds a node to the end of the list. O(1)
        /// </summary>
        public void Add(T value)
        {
#if DEBUG
            if (value.List != null)
            {
                throw new InvalidOperationException("Value is already in a list");
            }
#endif

            if (First == null)
            {
                First = value;
            }

            value.Prev = Last;

            if (Last != null)
            {
                Last.Next = value;
            }

            value.Next = null;

            Last = value;

#if DEBUG
            value.List = this;
#endif
            Count++;
        }

        /// <summary>
        /// Adds a node to the beginning of the list. O(1)
        /// </summary>
        public void InsertFirst(T value)
        {
#if DEBUG
            if (value.List != null)
            {
                throw new InvalidOperationException("Value is already in a list");
            }
#endif

            value.Prev = null;
            value.Next = First;

            if (First != null)
            {
                First.Prev = value;
            }

            First = value;

#if DEBUG
            value.List = this;
#endif
            Count++;
        }

        /// <summary>
        /// Adds a node before the given one. O(1)
        /// </summary>
        public void InsertBefore(T node, T value)
        {
#if DEBUG
            if (node.List != this)
            {
                throw new AccessViolationException("Node is not in this list");
            }

            if (value.List != null)
            {
                throw new InvalidOperationException("Value is already in a list");
            }
#endif

            if (First == node)
            {
                First = value;
            }

            if (node.Prev != null)
            {
                node.Prev.Next = value;
            }

            value.Prev = node.Prev;
            value.Next = node;
            node.Prev = value;

#if DEBUG
            value.List = this;
#endif
            Count++;
        }

        /// <summary>
        /// Adds a node after the given one. O(1)
        /// </summary>
        public void InsertAfter(T node, T value)
        {
#if DEBUG
            if (node.List != this)
            {
                throw new AccessViolationException("Node is not in this list");
            }

            if (value.List != null)
            {
                throw new InvalidOperationException("Value is already in a list");
            }
#endif

            if (Last == node)
            {
                Last = value;
            }

            if (node.Next != null)
            {
                node.Next.Prev = value;
            }

            value.Next = node.Next;
            value.Prev = node;
            node.Next = value;

#if DEBUG
            value.List = this;
#endif
            Count++;
        }

        /// <summary>
        /// Removes and returns a node from the list. O(1)
        /// </summary>
        public T Remove(T node)
        {
#if DEBUG
            if (node.List != this)
            {
                throw new AccessViolationException("Node is not in this list");
            }
#endif

            if (First == node)
            {
                First = node.Next;
            }

            if (Last == node)
            {
                Last = node.Prev;
            }

            if (node.Prev != null)
            {
                node.Prev.Next = node.Next;
            }

            if (node.Next != null)
            {
                node.Next.Prev = node.Prev;
            }

            node.Next = null;
            node.Prev = null;

#if DEBUG
            node.List = null;
#endif
            Count--;
            return node;
        }

        /// <summary>
        /// Removes and returns the first element. O(1)
        /// </summary>
        public T RemoveFirst()
        {
            if (First == null)
            {
                throw new AccessViolationException();
            }

            var result = First;
            if (result.Next != null)
            {
                result.Next.Prev = null;
            }

            First = result.Next;
            if (Last == result)
            {
                Last = null;
            }

            result.Next = null;
            result.Prev = null;

#if DEBUG
            result.List = null;
#endif
            Count--;
            return result;
        }

        /// <summary>
        /// Removes and returns the last element. O(1)
        /// </summary>
        public T RemoveLast()
        {
            if (Last == null)
            {
                throw new AccessViolationException();
            }

            var result = Last;
            if (result.Prev != null)
            {
                result.Prev.Next = null;
            }

            Last = result.Prev;
            if (First == result)
            {
                First = null;
            }

            result.Next = null;
            result.Prev = null;

#if DEBUG
            result.List = null;
#endif
            Count--;
            return result;
        }

        /// <summary>
        /// Gets the node after a given one. O(1)
        /// </summary>
        public T GetNext(T node)
        {
#if DEBUG
            if (node.List != this)
            {
                throw new AccessViolationException("Node is not in this list");
            }
#endif
            return node.Next;
        }

        /// <summary>
        /// Returns all of the values in the list. Slower due to foreach. O(n)
        /// </summary>
        public IEnumerable<T> GetValues()
        {
            var iter = First;
            while (iter != null)
            {
                yield return iter;
                iter = iter.Next;
            }
        }

        /// <summary>
        /// Applies an action to every member of the list. O(n)
        /// </summary>
        public void ForEach(Action<T> action)
        {
            var iter = First;
            while (iter != null)
            {
                action.Invoke(iter);
                iter = iter.Next;
            }
        }

        /// <summary>
        /// Clears the list. Does not free or modify values. O(1)
        /// </summary>
        public void Clear()
        {
            First = null;
            Last = null;
            Count = 0;
        }
    }
}
