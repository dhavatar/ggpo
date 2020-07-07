using GGPOSharp.Interfaces;
using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class SyncReplyMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.SyncReply;

        /// <summary>
        /// This should contain the random data presented by the sync request.
        /// </summary>
        public int RandomReply { get; set; }

        public override string GetLogMessage()
        {
            return $"sync-reply ({RandomReply})";
        }
    }
}
