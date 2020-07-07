namespace GGPOSharp.Network.Messages
{
    /// <summary>
    /// The different types of messages for GGPO.
    /// </summary>
    public enum MessageType : int
    {
        Invalid = 0,
        SyncRequest = 1,
        SyncReply = 2,
        Input = 3,
        QualityReport = 4,
        QualityReply = 5,
        KeepAlive = 6,
        InputAck = 7,
    }
}
