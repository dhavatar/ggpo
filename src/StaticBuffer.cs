using System.Diagnostics;

namespace GGPOSharp
{
    /// <summary>
    /// Generic static buffer array.
    /// </summary>
    /// <typeparam name="T">Type of item for the buffer to use.</typeparam>
    class StaticBuffer<T>
    {
        protected T[] data;

        /// <summary>
        /// The amount of items currently in the buffer.
        /// </summary>
        public int Size { get; protected set; }

        /// <summary>
        /// Constructor to create the buffer to the specified size.
        /// </summary>
        /// <param name="size">Size of the buffer.</param>
        public StaticBuffer(int size)
        {
            data = new T[size];
            Size = 0;
        }

        /// <summary>
        /// Access the buffer at the specified index.
        /// </summary>
        /// <param name="i">Index in the buffer.</param>
        /// <returns>Item from the specified index.</returns>
        public ref T this[int i]
        {
            get
            {
                Debug.Assert(i >= 0 && i < Size);
                return ref data[i];
            }
        }

        /// <summary>
        /// Adds an item to the buffer.
        /// </summary>
        /// <param name="item">Item to add to the buffer.</param>
        public void Push(in T item)
        {
            Debug.Assert(Size != data.Length - 1);
            data[Size++] = item;
        }
    }
}
