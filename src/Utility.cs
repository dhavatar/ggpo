using GGPOSharp.Interfaces;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace GGPOSharp
{
    /// <summary>
    /// Utility functions to help get the current time and other functions.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Retrieves the current system time in milliseconds.
        /// </summary>
        /// <returns>Current system time in milliseconds.</returns>
        public static long GetCurrentTime()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Returns the number of bytes of the msg.
        /// </summary>
        /// <param name="msg"><see cref="NetworkMessage"/> to size.</param>
        /// <returns>Size of the message in bytes.</returns>
        public static long GetMessageSize(NetworkMessage msg)
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, msg);
                return ms.Length;
            }
        }
        
        /// <summary>
        /// Converts any object into a byte array.
        /// </summary>
        /// <param name="obj">Generic object to convert.</param>
        /// <returns>A byte array representation of the object.</returns>
        public static byte[] GetByteArray(object obj)
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Simple checksum function stolen from wikipedia:
        /// http://en.wikipedia.org/wiki/Fletcher%27s_checksum
        /// </summary>
        /// <param name="data">Data to compute the checksum.</param>
        /// <param name="len">Block size of the computation.</param>
        /// <returns>A checksum generated from the data.</returns>
        public static int CreateChecksum(short[] data, int len)
        {
            int sum1 = 0xffff, sum2 = 0xffff;
            int index = 0;

            while (len > 0)
            {
                int tlen = len > 360 ? 360 : len;
                len -= tlen;
                do
                {
                    sum1 += data[index++];
                    sum2 += sum1;
                } while (--tlen > 0);
                sum1 = (sum1 & 0xffff) + (sum1 >> 16);
                sum2 = (sum2 & 0xffff) + (sum2 >> 16);
            }

            /* Second reduction step to reduce sums to 16 bits */
            sum1 = (sum1 & 0xffff) + (sum1 >> 16);
            sum2 = (sum2 & 0xffff) + (sum2 >> 16);
            return sum2 << 16 | sum1;
        }
    }
}
