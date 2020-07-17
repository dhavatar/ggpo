using System;
using System.Runtime.CompilerServices;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class QualityReplyMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.QualityReply;

        public uint Pong { get; set; }

        public QualityReplyMessage() { }

        public QualityReplyMessage(byte[] data)
        {
            Deserialize(data);
        }

        public override string GetLogMessage()
        {
            return "quality reply";
        }

        /// <summary>
        /// Serialize the network message into a custom byte array.
        /// </summary>
        /// <returns>A byte array representing the network message.</returns>
        public override byte[] Serialize()
        {
            var data = new byte[12];
            SerializeBase(ref data);

            Unsafe.CopyBlock(ref data[5], ref BitConverter.GetBytes(Pong)[0], 4);

            return data;
        }

        /// <summary>
        /// Deserialize a byte array into a <see cref="NetworkMessage"/>.
        /// </summary>
        /// <param name="data">Byte array to convert.</param>
        public override void Deserialize(byte[] data)
        {
            DeserializeBase(data);

            Pong = BitConverter.ToUInt32(data, 5);
        }
    }
}
