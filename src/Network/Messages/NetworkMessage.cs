using GGPOSharp.Network.Messages;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GGPOSharp
{
    /// <summary>
    /// Base abstract class for the network messages that will be sent.
    /// </summary>
    [Serializable]
    public abstract class NetworkMessage
    {
        /// <summary>
        /// The type of data for this message.
        /// </summary>
        public virtual MessageType Type { get; private set; }

        public ushort Magic { get; set; }

        /// <summary>
        /// Sequence number in the network packet.
        /// </summary>
        public ushort SequenceNumber { get; set; }

        /// <summary>
        /// Retrieves the name of the message and other properties for logging.
        /// </summary>
        /// <returns>A string information about the message.</returns>
        public abstract string GetLogMessage();

        /// <summary>
        /// Serialize the network message into a custom byte array.
        /// </summary>
        /// <returns>A byte array representing the network message.</returns>
        public abstract byte[] Serialize();

        /// <summary>
        /// Deserialize a byte array into a <see cref="NetworkMessage"/>.
        /// </summary>
        /// <param name="data">Byte array to convert.</param>
        /// <returns>A <see cref="NetworkMessage"/> from the byte array.</returns>
        public abstract void Deserialize(byte[] data);

        /// <summary>
        /// Helper method to serialize the magic, sequence, and type into the first 5 bytes of the byte array.
        /// </summary>
        /// <param name="data">Byte array to fill with the properties.</param>
        protected void SerializeBase(ref byte[] data)
        {
            Debug.Assert(data.Length >= 5);

            Unsafe.CopyBlock(ref data[0], ref BitConverter.GetBytes(Magic)[0], 2);
            Unsafe.CopyBlock(ref data[2], ref BitConverter.GetBytes(SequenceNumber)[0], 2);
            data[4] = (byte)Type;
        }

        /// <summary>
        /// Helper method to deserialize the magic, sequence, and type from the first 5 bytes of the byte array.
        /// </summary>
        /// <param name="data">Byte array to get the properties.</param>
        protected void DeserializeBase(byte[] data)
        {
            Debug.Assert(data.Length >= 5);

            Magic = BitConverter.ToUInt16(data, 0);
            SequenceNumber = BitConverter.ToUInt16(data, 2);
            Type = (MessageType)data[4];
        }
    }
}
