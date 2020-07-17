using GGPOSharp.Interfaces;
using System;

namespace GGPOSharp.Network.Messages
{
    /// <summary>
    /// Empty message used to keep the network connection alive by sending
    /// network packets to the player.
    /// </summary>
    [Serializable]
    class KeepAliveMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.KeepAlive;

        public KeepAliveMessage() { }

        public KeepAliveMessage(byte[] data)
        {
            Deserialize(data);
        }

        public override string GetLogMessage()
        {
            return "keep alive";
        }

        /// <summary>
        /// Serialize the network message into a custom byte array.
        /// </summary>
        /// <returns>A byte array representing the network message.</returns>
        public override byte[] Serialize()
        {
            var data = new byte[5];
            SerializeBase(ref data);
            return data;
        }

        /// <summary>
        /// Deserialize a byte array into a <see cref="NetworkMessage"/>.
        /// </summary>
        /// <param name="data">Byte array to convert.</param>
        public override void Deserialize(byte[] data)
        {
            DeserializeBase(data);
        }
    }
}
