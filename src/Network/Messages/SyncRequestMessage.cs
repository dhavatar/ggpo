using System;
using System.Runtime.CompilerServices;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class SyncRequestMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.SyncRequest;

        /// <summary>
        /// The sync reply should reply back with his random data.
        /// </summary>
        public uint RandomRequest { get; set; }

        public ushort RemoteMagic { get; set; }

        public byte RemoteEndpoint { get; set; }

        public SyncRequestMessage() { }

        public SyncRequestMessage(byte[] data)
        {
            Deserialize(data);
        }

        public override string GetLogMessage()
        {
            return $"sync-request ({RandomRequest})";
        }

        /// <summary>
        /// Serialize the network message into a custom byte array.
        /// </summary>
        /// <returns>A byte array representing the network message.</returns>
        public override byte[] Serialize()
        {            
            var data = new byte[12];
            SerializeBase(ref data);

            Unsafe.CopyBlock(ref data[5], ref BitConverter.GetBytes(RandomRequest)[0], 4);
            Unsafe.CopyBlock(ref data[9], ref BitConverter.GetBytes(RemoteMagic)[0], 2);
            data[11] = RemoteEndpoint;

            return data;
        }

        /// <summary>
        /// Deserialize a byte array into a <see cref="NetworkMessage"/>.
        /// </summary>
        /// <param name="data">Byte array to convert.</param>
        public override void Deserialize(byte[] data)
        {
            DeserializeBase(data);

            RandomRequest = BitConverter.ToUInt32(data, 5);
            RemoteMagic = BitConverter.ToUInt16(data, 9);
            RemoteEndpoint = data[11];
        }
    }
}
