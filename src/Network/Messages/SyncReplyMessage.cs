using System;
using System.Runtime.CompilerServices;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class SyncReplyMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.SyncReply;

        /// <summary>
        /// This should contain the random data presented by the sync request.
        /// </summary>
        public uint RandomReply { get; set; }

        public SyncReplyMessage() { }

        public SyncReplyMessage(byte[] data)
        {
            Deserialize(data);
        }

        public override string GetLogMessage()
        {
            return $"sync-reply ({RandomReply})";
        }

        /// <summary>
        /// Serialize the network message into a custom byte array.
        /// </summary>
        /// <returns>A byte array representing the network message.</returns>
        public override byte[] Serialize()
        {
            var data = new byte[9];
            SerializeBase(ref data);

            Unsafe.CopyBlock(ref data[5], ref BitConverter.GetBytes(RandomReply)[0], 4);

            return data;
        }

        /// <summary>
        /// Deserialize a byte array into a <see cref="NetworkMessage"/>.
        /// </summary>
        /// <param name="data">Byte array to convert.</param>
        public override void Deserialize(byte[] data)
        {
            DeserializeBase(data);

            RandomReply = BitConverter.ToUInt32(data, 5);
        }
    }
}
