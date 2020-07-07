using System;

namespace GGPOSharp.Network.Events
{
    [Serializable]
    public class SynchronizingEvent : UdpProtocolEvent
    {
        public override Type EventType { get; set; } = Type.Synchronizing;

        public int Total { get; set; }

        public int Count { get; set; }
    }
}
