namespace GGPOSharp
{
    /// <summary>
    /// Types of error codes from GGPO.
    /// </summary>
    public enum GGPOErrorCode : byte
    {
        OK = 0,
        Success = 0,
        GeneralFailure = 255,
        InvalidSession = 1,
        InvalidPlayerHandle = 2,
        PlayerOutOfRange = 3,
        PredictionThreshold = 4,
        Unsupported = 5,
        NotSynchronized = 6,
        InRollback = 7,
        InputDropped = 8,
        PlayerDisconnected = 9,
        TooManySpectators = 10,
        InvalidRequest = 11
    }
}
