using System;

namespace GGPOSharp.Network.Events
{
    /// <summary>
    /// Simple UDP event message to send.
    /// </summary>
    [Serializable]
    public class UdpProtocolEvent
    {
        public enum Type
        {
            Unknown = -1,
            Connected,
            Synchronizing,
            Synchronized,
            Input,
            Disconnected,
            NetworkInterrupted,
            NetworkResumed,
        };

        /// <summary>
        /// Type of network event message.
        /// </summary>
        public virtual Type EventType { get; set; }

        /// <summary>
        /// Constructor that initializes the event type.
        /// </summary>
        /// <param name="type"><see cref="Type"/> of event.</param>
        public UdpProtocolEvent(Type type)
        {
            EventType = type;
        }
    }
}
