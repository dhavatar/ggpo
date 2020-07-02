using GGPOSharp.Interfaces;
using GGPOSharp.Logger;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GGPOSharp
{
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

        public bool this[int i]
        {
            get
            {
                return (bits[i / 8] & (1 << (i % 8))) != 0;
            }
        }

        public void Set(int i)
        {
            bits[i / 8] |= (byte)(1 << (i % 8));
        }

        public void Clear(int i)
        {
            bits[i / 8] &= (byte)~(1 << (i % 8));
        }

        public void Erase()
        {
            Array.Clear(bits, 0, bits.Length);
        }

        public string ToString(bool show_frame = true)
        {
            Debug.Assert(size > 0);

            string retVal;
            if (show_frame)
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

        public void Log(string prefix, bool show_frame = true)
        {
            Logger.Log(prefix + ToString(show_frame));
        }

        public bool Equal(in GameInput other, bool bitsonly = false)
        {
            if (!bitsonly && frame != other.frame)
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
            return (bitsonly || frame == other.frame) &&
                   size == other.size &&
                   bits.SequenceEqual(other.bits);
        }
    }
}
