using System;

namespace GGPOSharp.Network.Events
{
    [Serializable]
    public class NetworkInterruptedEvent : UdpProtocolEvent
    {
        public override Type EventType { get; set; } = Type.NetworkInterrupted;

        public int DisconnectTimeout { get; set; }
    }
}
