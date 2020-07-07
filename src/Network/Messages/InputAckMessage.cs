using GGPOSharp.Interfaces;
using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class InputAckMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.InputAck;

        public int AckFrame { get; set; }

        public override string GetLogMessage()
        {
            return "input ack";
        }
    }
}
