using System;

namespace GGPOSharp.Network.Events
{
    [Serializable]
    public class InputEvent : UdpProtocolEvent
    {
        public override Type EventType { get; set; } = Type.Input;

        public GameInput Input { get; set; }
    }
}
