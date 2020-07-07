using GGPOSharp.Interfaces;
using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class QualityReplyMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.QualityReply;

        public long Pong { get; set; }

        public override string GetLogMessage()
        {
            return "quality reply";
        }
    }
}
