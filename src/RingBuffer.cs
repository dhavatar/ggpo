using System.Diagnostics;

namespace GGPOSharp
{
    /// <summary>
    /// Generic ring buffer.
    /// </summary>
    /// <typeparam name="T">Type of item to store in the ring buffer.</typeparam>
    class RingBuffer<T>
    {
        /// <summary>
        /// Current number of items in the ring buffer.
        /// </summary>
        public int Size
        {
            get
            {
                if (head >= tail)
                {
                    return head - tail;
                }
                return head + (Capacity - tail);
            }
        }

        /// <summary>
        /// Total capacity of the ring buffer.
        /// </summary>
        public int Capacity => data.Length;

        /// <summary>
        /// Returns true if there's no item in the buffer.
        /// </summary>
        public bool IsEmpty => Size == 0;

        /// <summary>
        /// Returns true if the amount of items fills the buffer.
        /// </summary>
        public bool IsFull => Size == Capacity;

        protected readonly T[] data;
        protected int head = 0;
        protected int tail = 0;

        /// <summary>
        /// Constructor for the ring buffer with the specified size.
        /// </summary>
        /// <param name="capacity">Size of the ring buffer.</param>
        public RingBuffer(int capacity)
        {
            data = new T[capacity];
        }

        /// <summary>
        /// Returns the front item in the ring buffer.
        /// </summary>
        /// <returns>Front item in the buffer.</returns>
        public ref T Front()
        {
            Debug.Assert(Size != Capacity);
            return ref data[tail];
        }

        /// <summary>
        /// Retrieves the i-th item in the ring buffer.
        /// </summary>
        /// <param name="i">Index in the ring buffer.</param>
        /// <returns>Item located at the specified index.</returns>
        public ref T this[int i]
        {
            get
            {
                Debug.Assert(i < Size);
                return ref data[(tail + i) % Capacity];
            }
        }

        /// <summary>
        /// Removes the front item in the ring buffer.
        /// </summary>
        public void Pop()
        {
            Debug.Assert(Size != Capacity);
            tail = (tail + 1) % Capacity;
        }

        /// <summary>
        /// Adds an item to the back of the ring buffer.
        /// </summary>
        /// <param name="item"></param>
        public void Push(in T item)
        {
            Debug.Assert(Size != Capacity - 1);
            data[head] = item;
            head = (head + 1) % Capacity;
        }
    }
}
