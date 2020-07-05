using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class QualityReportMessage
    {
        /// <summary>
        /// The other player's frame advantage compared to the current player.
        /// </summary>
        public byte FrameAdvantage { get; set; }

        public int Ping { get; set; }
    }
}
