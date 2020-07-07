using GGPOSharp.Interfaces;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace GGPOSharp
{
    /// <summary>
    /// Utility functions to help get the current time and other functions.
    /// </summary>
    static class Utility
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
    }
}
