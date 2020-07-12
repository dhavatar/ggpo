using System;

namespace GGPOSharp.Network.Messages
{
    [Serializable]
    public class QualityReportMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.QualityReport;

        /// <summary>
        /// The other player's frame advantage compared to the current player.
        /// </summary>
        public byte FrameAdvantage { get; set; }

        public long Ping { get; set; }

        public override string GetLogMessage()
        {
            return "quality report";
        }
    }
}
