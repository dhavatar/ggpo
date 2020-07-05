using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class InputAckMessage
    {
        public int AckFrame { get; set; }
    }
}
