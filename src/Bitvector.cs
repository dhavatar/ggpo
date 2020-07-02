using System.Diagnostics;

namespace GGPOSharp
{
    /// <summary>
    /// Class containing operations on bit vectors represented by a byte vector.
    /// </summary>
    public static class Bitvector
    {
        public const int NibbleSize = 8;

        /// <summary>
        /// Sets a bit in the bit vector.
        /// </summary>
        /// <param name="vector">Bit vector to modify.</param>
        /// <param name="offset">Reference integer index to the vector. Will increment by 1.</param>
        public static void SetBit(byte[] vector, ref int offset)
        {
            vector[offset / 8] |= (byte)(1 << (offset % 8));
            offset++;
        }

        /// <summary>
        /// Clears a bit in the bit vector.
        /// </summary>
        /// <param name="vector">Bit vector to modify.</param>
        /// <param name="offset">Reference integer index to the vector. Will increment by 1.</param>
        public static void ClearBit(byte[] vector, ref int offset)
        {
            vector[offset / 8] &= (byte)~(1 << (offset % 8));
            offset++;
        }

        /// <summary>
        /// Writes a nibblet in the bit vector.
        /// </summary>
        /// <param name="vector">Bit vector to modify.</param>
        /// <param name="nibble"></param>
        /// <param name="offset">Reference integer index to the vector. Will increment by 8.</param>
        public static void WriteNibblet(byte[] vector, int nibble, ref int offset)
        {
            Debug.Assert(nibble < (1 << NibbleSize));
            for (int i = 0; i < NibbleSize; i++)
            {
                if ((nibble & (1 << i)) != 0)
                {
                    SetBit(vector, ref offset);
                }
                else
                {
                    ClearBit(vector, ref offset);
                }
            }
        }

        /// <summary>
        /// Reads a bit in the bit vector.
        /// </summary>
        /// <param name="vector">Bit vector to read.</param>
        /// <param name="offset">Reference integer index to the vector. Will increment by 1.</param>
        public static int ReadBit(byte[] vector, ref int offset)
        {
            int retval = vector[offset / 8] & (1 << (offset % 8));
            offset++;
            return retval;
        }

        /// <summary>
        /// Reads a nibblet in the bit vector.
        /// </summary>
        /// <param name="vector">Bit vector to read.</param>
        /// <param name="offset">Reference integer index to the vector. Will increment by 8.</param>
        public static int ReadNibblet(byte[] vector, ref int offset)
        {
            int nibblet = 0;
            for (int i = 0; i < NibbleSize; i++)
            {
                nibblet |= (ReadBit(vector, ref offset) << i);
            }
            return nibblet;
        }
    }
}
