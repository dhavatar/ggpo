using GGPOSharp.Interfaces;
using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class SyncRequestMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.SyncRequest;

        /// <summary>
        /// The sync reply should reply back with his random data.
        /// </summary>
        public int RandomRequest { get; set; }

        public byte RemoteEndpoint { get; set; }

        public override string GetLogMessage()
        {
            return $"sync-request ({RandomRequest})";
        }
    }
}
