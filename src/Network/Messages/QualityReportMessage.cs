using System;
using System.Runtime.CompilerServices;

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

        public uint Ping { get; set; }

        public QualityReportMessage() { }

        public QualityReportMessage(byte[] data)
        {
            Deserialize(data);
        }

        public override string GetLogMessage()
        {
            return "quality report";
        }

        /// <summary>
        /// Serialize the network message into a custom byte array.
        /// </summary>
        /// <returns>A byte array representing the network message.</returns>
        public override byte[] Serialize()
        {
            var data = new byte[12];
            SerializeBase(ref data);

            data[5] = FrameAdvantage;
            Unsafe.CopyBlock(ref data[6], ref BitConverter.GetBytes(Ping)[0], 4);

            return data;
        }

        /// <summary>
        /// Deserialize a byte array into a <see cref="NetworkMessage"/>.
        /// </summary>
        /// <param name="data">Byte array to convert.</param>
        public override void Deserialize(byte[] data)
        {
            DeserializeBase(data);

            FrameAdvantage = data[5];
            Ping = BitConverter.ToUInt32(data, 6);
        }
    }
}
