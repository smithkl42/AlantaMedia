using System.Collections.Generic;
using System.Diagnostics;

namespace Alanta.Client.Media.Jpeg
{
    public class BufferPoolDictionary<T>
    {
        private readonly Dictionary<int, BufferPool<T>> _dictionary;

        public BufferPoolDictionary()
        {
            _dictionary = new Dictionary<int, BufferPool<T>>();
        }

        public T[][] GetNext(int size, bool clear = false)
        {
            BufferPool<T> pool;
            if (!_dictionary.TryGetValue(size, out pool))
            {
                pool = new BufferPool<T>(size);
                _dictionary.Add(size, pool);
            }
            return pool.GetNext(clear);
        }

        public void Recycle(T[][] buffer)
        {
            BufferPool<T> pool;
            if (!_dictionary.TryGetValue(buffer.Length, out pool))
            {
                Debug.WriteLine("Unexpectedly recycled a buffer that didn't already have a buffer pool of the correct size.");
                pool = new BufferPool<T>(buffer.Length);
            }
            pool.Recycle(buffer);
        }
    }
}
