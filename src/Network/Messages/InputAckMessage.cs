using System;
using System.Runtime.CompilerServices;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class InputAckMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.InputAck;

        public int AckFrame { get; set; }

        public InputAckMessage() { }

        public InputAckMessage(byte[] data)
        {
            Deserialize(data);
        }

        public override string GetLogMessage()
        {
            return "input ack";
        }

        /// <summary>
        /// Serialize the network message into a custom byte array.
        /// </summary>
        /// <returns>A byte array representing the network message.</returns>
        public override byte[] Serialize()
        {
            var data = new byte[9];
            SerializeBase(ref data);

            Unsafe.CopyBlock(ref data[5], ref BitConverter.GetBytes(AckFrame)[0], 4);

            return data;
        }

        /// <summary>
        /// Deserialize a byte array into a <see cref="NetworkMessage"/>.
        /// </summary>
        /// <param name="data">Byte array to convert.</param>
        public override void Deserialize(byte[] data)
        {
            DeserializeBase(data);

            AckFrame = BitConverter.ToInt32(data, 5) >> 1;
        }
    }
}
