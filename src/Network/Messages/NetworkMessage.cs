using GGPOSharp.Network.Messages;
using System;

namespace GGPOSharp
{
    /// <summary>
    /// Base abstract class for the network messages that will be sent.
    /// </summary>
    [Serializable]
    public abstract class NetworkMessage
    {
        /// <summary>
        /// The type of data for this message.
        /// </summary>
        public abstract MessageType Type { get; }

        public ushort Magic { get; set; }

        /// <summary>
        /// Sequence number in the network packet.
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Retrieves the name of the message and other properties for logging.
        /// </summary>
        /// <returns>A string information about the message.</returns>
        public abstract string GetLogMessage();
    }
}
