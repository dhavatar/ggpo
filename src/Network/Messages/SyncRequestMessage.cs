using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class SyncRequestMessage
    {
        /// <summary>
        /// The sync reply should reply back with his random data.
        /// </summary>
        public int RandomRequest { get; set; }

        public byte RemoteEndpoint { get; set; }
    }
}
