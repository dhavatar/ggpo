using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class InputMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.Input;

        public NetworkConnectStatus[] PeerConnectStatus { get; set; } = new NetworkConnectStatus[Constants.MaxPlayers];

        public int StartFrame { get; set; }

        public bool DisconnectRequested { get; set; }

        public int AckFrame { get; set; }

        public ushort NumBits { get; set; }

        public uint InputSize { get; set; }

        public byte[] Bits { get; set; } = new byte[Constants.MaxCompressedBits / 8];

        public override string GetLogMessage()
        {
            return $"game-compressed-input {StartFrame} (+ {NumBits} bits)";
        }
    }
}
