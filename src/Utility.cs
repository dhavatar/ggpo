using GGPOSharp.Network.Messages;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        public static uint GetCurrentTime()
        {
            return timeGetTime();
        }

        /// <summary>
        /// Returns the number of bytes of the msg.
        /// </summary>
        /// <param name="msg"><see cref="NetworkMessage"/> to size.</param>
        /// <returns>Size of the message in bytes.</returns>
        public static long GetMessageSize(NetworkMessage msg)
        {
            using (var ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();
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
            using (var ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Converts a NetworkMessage into a byte array.
        /// </summary>
        /// <param name="msg"><see cref="NetworkMessage"/> to convert.</param>
        /// <returns>A byte array representation of the network message.</returns>
        public static byte[] GetByteArray(NetworkMessage msg)
        {
            return msg.Serialize();
        }

        /// <summary>
        /// Deserializes a byte array into the proper <see cref="NetworkMessage"/>.
        /// </summary>
        /// <typeparam name="T">Class to convert the byte array into.</typeparam>
        /// <param name="data">Byte array of the class information.</param>
        /// <returns><see cref="NetworkMessage"/> class from the converted byte array.</returns>
        public static NetworkMessage Deserialize(byte[] data)
        {
            // Get the 5th byte to determine the message type
            var type = (MessageType)data[4];
            switch (type)
            {
                case MessageType.Input:
                    return new InputMessage(data);

                case MessageType.InputAck:
                    return new InputAckMessage(data);

                case MessageType.KeepAlive:
                    return new KeepAliveMessage(data);

                case MessageType.QualityReply:
                    return new QualityReplyMessage(data);

                case MessageType.QualityReport:
                    return new QualityReportMessage(data);

                case MessageType.SyncReply:
                    return new SyncReplyMessage(data);

                case MessageType.SyncRequest:
                    return new SyncRequestMessage(data);
            }

            // Shouldn't reach here
            return null;
        }

        /// <summary>
        /// Byte array version of the checksum formula.
        /// </summary>
        /// <param name="data">Byte array data to compute the checksum.</param>
        /// <returns>A checksum generated from the data.</returns>
        public static int CreateChecksum(byte[] data)
        {
            var buffer = Utility.GetByteArray(data);
            short[] sdata = new short[(int)Math.Ceiling(buffer.Length / 2.0)];
            Buffer.BlockCopy(buffer, 0, sdata, 0, buffer.Length);

            return CreateChecksum(sdata, sdata.Length / 2);
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

        [DllImport("winmm.dll")]
        public static extern UInt32 timeGetTime();
    }
}
