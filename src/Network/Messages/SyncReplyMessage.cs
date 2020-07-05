using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class SyncReplyMessage
    {
        /// <summary>
        /// This should contain the random data presented by the sync request.
        /// </summary>
        public int RandomReply { get; set; }
    }
}
