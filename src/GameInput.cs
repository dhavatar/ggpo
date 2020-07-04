using GGPOSharp.Interfaces;
using GGPOSharp.Logger;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GGPOSharp
{
    /// <summary>
    /// Class containing the inputs for the game in bit form.
    /// </summary>
    public struct GameInput
    {
        // TODO: Allow other people to change this logger
        private static readonly ILog Logger = ConsoleLogger.GetLogger();

        // MaxBytes * MaxPlayers * 8 must be less than
        // 2^NibbleSize (see Bitvector)

        public const int MaxBytes = 8;
        public const int MaxPlayers = 2;

        public const int NullFrame = -1;

        public int frame;
        public uint size;
        public byte[] bits;

        public bool IsNull() => frame == NullFrame;

        /// <summary>
        /// Constructor that includes the frame and input bit information with an offset on how to copy the bits..
        /// </summary>
        /// <param name="frame">The frame for this input.</param>
        /// <param name="bits">Source input bits for this frame of input.</param>
        /// <param name="size">The size of the input bit array to copy.</param>
        /// <param name="offset">Offset in the destination array on where the source bits will copy to.</param>
        public GameInput(int frame, byte[] bits, uint size, int offset)
        {
            Debug.Assert(size > 0);
            Debug.Assert(size <= MaxBytes);

            this.frame = frame;
            this.size = size;
            this.bits = new byte[MaxBytes * MaxPlayers];
            
            if (bits != null)
            {
                Array.Copy(bits, 0, this.bits, offset, size);
            }
        }

        /// <summary>
        /// Constructor that includes the frame and input bit information.
        /// </summary>
        /// <param name="frame">The frame for this input.</param>
        /// <param name="bits">Source input bits for this frame of input.</param>
        /// <param name="size">The size of the input bit array to copy.</param>
        public GameInput(int frame, byte[] bits, uint size)
        {
            Debug.Assert(size > 0);
            Debug.Assert(size <= MaxBytes * MaxPlayers);

            this.frame = frame;
            this.size = size;
            this.bits = new byte[MaxBytes * MaxPlayers];

            if (bits != null)
            {
                Array.Copy(bits, this.bits, size);
            }
        }

        /// <summary>
        /// Gets the byte position from the array.
        /// </summary>
        /// <param name="i">Byte position to retrieve from the array.</param>
        /// <returns>The byte information from the array.</returns>
        public bool this[int i]
        {
            get
            {
                return (bits[i / 8] & (1 << (i % 8))) != 0;
            }
        }

        /// <summary>
        /// Sets the specified byte to 1.
        /// </summary>
        /// <param name="i">Byte position to set.</param>
        public void Set(int i)
        {
            bits[i / 8] |= (byte)(1 << (i % 8));
        }

        /// <summary>
        /// Clears the specified byte to 0.
        /// </summary>
        /// <param name="i">Byte position to clear.</param>
        public void Clear(int i)
        {
            bits[i / 8] &= (byte)~(1 << (i % 8));
        }

        /// <summary>
        /// Clears the input and resets everything to 0.
        /// </summary>
        public void Erase()
        {
            Array.Clear(bits, 0, bits.Length);
        }

        /// <summary>
        /// Converts the game input into a string form.
        /// </summary>
        /// <param name="showFrame">True if the frame information should be included in the string.</param>
        /// <returns>The string form of the game input byte array.</returns>
        public string ToString(bool showFrame = true)
        {
            Debug.Assert(size > 0);

            string retVal;
            if (showFrame)
            {
                retVal = $"(frame:{frame} size:{size} ";
            }
            else
            {
                retVal = $"(size:{size} ";
            }

            var builder = new StringBuilder(retVal);
            for (var i = 0; i < size; i++)
            {
                builder.AppendFormat("{0:x2}", bits[size]);
            }

            builder.Append(")");
            return builder.ToString();
        }

        /// <summary>
        /// Helper method to log game input state.
        /// </summary>
        /// <param name="prefix">String prefix to add to the string log.</param>
        /// <param name="showFrame">True if the frame information should be included. Defaults to true.</param>
        public void Log(string prefix, bool showFrame = true)
        {
            Logger.Log(prefix + ToString(showFrame));
        }

        /// <summary>
        /// Comparison function between two game inputs.
        /// </summary>
        /// <param name="other">The <see cref="GameInput"/> to compare against.</param>
        /// <param name="bitsOnly">True if only the bits will be compared and ignores the frame. Defaults to false.</param>
        /// <returns>True if the two game inputs bits match and the frames match (if bitsOnly is false).</returns>
        public bool Equal(in GameInput other, bool bitsOnly = false)
        {
            if (!bitsOnly && frame != other.frame)
            {
                Logger.Log($"frames don't match: {frame}, {other.frame}\n");
            }
            if (size != other.size)
            {
                Logger.Log($"sizes don't match: {size}, {other.size}\n");
            }
            if (bits.SequenceEqual(other.bits))
            {
                Logger.Log("bits don't match\n");
            }

            Debug.Assert(size > 0 && other.size > 0);
            return (bitsOnly || frame == other.frame) &&
                   size == other.size &&
                   bits.SequenceEqual(other.bits);
        }
    }
}
