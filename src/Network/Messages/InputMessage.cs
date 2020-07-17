using System;
using System.Runtime.CompilerServices;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class InputMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.Input;

        public NetworkConnectStatus[] PeerConnectStatus { get; set; } = new NetworkConnectStatus[Constants.MaxPlayers];

        public uint StartFrame { get; set; }

        public bool DisconnectRequested { get; set; }

        public int AckFrame { get; set; }

        public ushort NumBits { get; set; }

        public byte InputSize { get; set; }

        public byte[] Bits { get; set; } = new byte[Constants.MaxCompressedBits / 8];

        public InputMessage() { }

        public InputMessage(byte[] data)
        {
            Deserialize(data);
        }

        public override string GetLogMessage()
        {
            return $"game-compressed-input {StartFrame} (+ {NumBits} bits)";
        }

        /// <summary>
        /// Serialize the network message into a custom byte array.
        /// </summary>
        /// <returns>A byte array representing the network message.</returns>
        public override byte[] Serialize()
        {
            var data = new byte[544];
            SerializeBase(ref data);

            for (int i = 0; i < PeerConnectStatus.Length; i++)
            {
                if (PeerConnectStatus[i] != null)
                {
                    Unsafe.CopyBlock(ref data[5 + (i * 4)], ref BitConverter.GetBytes(PeerConnectStatus[i].data)[0], 4);
                }
                else
                {
                    Unsafe.InitBlock(ref data[5 + (i * 4)], 0, 4);
                }
            }

            int offset = 5 + (PeerConnectStatus.Length * 4);

            Unsafe.CopyBlock(ref data[offset], ref BitConverter.GetBytes(StartFrame)[0], 4);
            offset += 4;

            int disconnectAckFrame = 0;
            disconnectAckFrame = (disconnectAckFrame & ~1) | (DisconnectRequested ? 1 : 0);
            disconnectAckFrame = (disconnectAckFrame & 1) | (AckFrame << 1);
            Unsafe.CopyBlock(ref data[offset], ref BitConverter.GetBytes(disconnectAckFrame)[0], 4);
            offset += 4;

            Unsafe.CopyBlock(ref data[offset], ref BitConverter.GetBytes(NumBits)[0], 2);
            offset += 2;

            data[offset++] = InputSize;

            Unsafe.CopyBlock(ref data[offset], ref Bits[0], Constants.MaxCompressedBits / 8);

            return data;
        }

        /// <summary>
        /// Deserialize a byte array into a <see cref="NetworkMessage"/>.
        /// </summary>
        /// <param name="data">Byte array to convert.</param>
        public override void Deserialize(byte[] data)
        {
            DeserializeBase(data);

            for (int i = 0; i < PeerConnectStatus.Length; i++)
            {
                PeerConnectStatus[i] = new NetworkConnectStatus(BitConverter.ToInt32(data, 5 + (i * 4)));
            }

            int offset = 5 + (PeerConnectStatus.Length * 4);

            StartFrame = BitConverter.ToUInt32(data, offset);
            offset += 4;

            int disconnectAckFrame = BitConverter.ToInt32(data, offset);
            DisconnectRequested = (disconnectAckFrame & 1) != 0;
            AckFrame = disconnectAckFrame >> 1;
            offset += 4;

            NumBits = BitConverter.ToUInt16(data, offset);
            offset += 2;

            InputSize = data[offset++];

            Unsafe.CopyBlock(ref Bits[0], ref data[offset], Constants.MaxCompressedBits / 8);
        }
    }
}
