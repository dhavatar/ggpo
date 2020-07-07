using GGPOSharp.Network.Messages;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace GGPOSharp.Interfaces
{
    /// <summary>
    /// Base interface for the network messages that will be sent.
    /// </summary>
    [Serializable]
    public abstract class NetworkMessage
    {
        /// <summary>
        /// The type of data for this message.
        /// </summary>
        public abstract MessageType Type { get; }

        public ushort Magic { get; set; }

        /// <summary>
        /// Sequence number in the network packet.
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Retrieves the name of the message and other properties for logging.
        /// </summary>
        /// <returns>A string information about the message.</returns>
        public abstract string GetLogMessage();

        /// <summary>
        /// Converts the network message into a byte array to send through the network.
        /// </summary>
        /// <returns>A byte array representation of the network message.</returns>
        public virtual byte[] ToByteArray()
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, this);
                return ms.ToArray();
            }
        }
    }
}
