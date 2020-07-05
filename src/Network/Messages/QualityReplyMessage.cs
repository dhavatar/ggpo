using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class QualityReplyMessage
    {
        public int Pong { get; set; }
    }
}
