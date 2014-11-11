using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Alanta.Client.Media.Jpeg
{
    /// <summary>
    ///     Retrieves, stores and recycles reusable objects (like SocketAsyncEventArgs) to help reduce the amount of time spent
    ///     in Garbage Collection.
    /// </summary>
    /// <typeparam name="T">The type of the object to be reused.</typeparam>
    public class BufferPool<T>
    {
        #region Constructors

        /// <summary>
        ///     Initializes the BufferPool class with a fixed size for the buffers that it returns.
        /// </summary>
        public BufferPool(int size)
        {
            this.size = size;
            stack = new Stack<T[][]>();
        }

        #endregion

        #region Fields and Properties

        private readonly int size;
        private readonly Stack<T[][]> stack;
        private long buffersCreated;
        private long buffersRecycled;
        private long buffersReused;

        #endregion

        /// <summary>
        ///     Retrieves the next available instance of the targeted class, or creates one if none is available.
        /// </summary>
        /// <returns>An instance of the targeted class.</returns>
        public T[][] GetNext(bool clear = false)
        {
            T[][] obj = null;
            lock (stack)
            {
                if (stack.Count == 0)
                {
#if DEBUG
                    if (++buffersCreated%100 == 0)
                    {
                        Debug.WriteLine(
                            "Object Pool for type {0}: Created={1}; Reused={2}; Recycled={3}; Outstanding={4}",
                            typeof (T).FullName, buffersCreated, buffersRecycled, buffersReused,
                            (buffersCreated + buffersReused) - buffersRecycled);
                    }
#endif
                    var arr = new T[size][];
                    for (var i = 0; i < size; i++)
                    {
                        arr[i] = new T[size];
                    }
                    return arr;
                }
                buffersReused++;
                obj = stack.Pop();
                if (clear) Array.Clear(obj, 0, obj.Length);
            }
            return obj;
        }

        /// <summary>
        ///     Pushes the instance of the targeted class back onto the stack, making it available for re-use.
        /// </summary>
        /// <param name="obj">The instance of the targeted class to be recycled.</param>
        public void Recycle(T[][] obj)
        {
            if (obj != null)
            {
                lock (stack)
                {
                    stack.Push(obj);
#if DEBUG
                    buffersRecycled++;
#endif
                }
            }
        }
    }
}