using GGPOSharp.Interfaces;
using System;

namespace GGPOSharp.Network.Messages
{
    /// <summary>
    /// Empty message used to keep the network connection alive by sending
    /// network packets to the player.
    /// </summary>
    [Serializable]
    class KeepAliveMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.KeepAlive;

        public override string GetLogMessage()
        {
            return "keep alive";
        }
    }
}
