using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class InputMessage
    {
        public NetworkConnectStatus[] PeerConnectStatus { get; set; } = new NetworkConnectStatus[Constants.MaxPlayers];

        public int StartFrame { get; set; }

        public bool DisconnectRequested { get; set; }

        public int AckFrame { get; set; }

        public short NumBits { get; set; }

        public int InputSize { get; set; }

        public byte[] Bits { get; set; } = new byte[Constants.MaxCompressedBits];
    }
}
