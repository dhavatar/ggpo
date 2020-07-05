using System;

namespace GGPOSharp
{
    /// <summary>
    /// Types of error codes from GGPO.
    /// </summary>
    public enum GGPOErrorCode : byte
    {
        OK                  = 0,
        Success             = 0,
        GeneralFailure      = 255,
        InvalidSession      = 1,
        InvalidPlayerHandle = 2,
        PlayerOutOfRange    = 3,
        PredictionThreshold = 4,
        Unsupported         = 5,
        NotSynchronized     = 6,
        InRollback          = 7,
        InputDropped        = 8,
        PlayerDisconnected  = 9,
        TooManySpectators   = 10,
        InvalidRequest       = 11
    }

    /// <summary>
    /// Represents GGPO errors that can occur.
    /// </summary>
    public class GGPOException : Exception
    {
        /// <summary>
        /// Type of error for the exception.
        /// </summary>
        public GGPOErrorCode ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the GGPO exception with an error code.
        /// </summary>
        /// <param name="code"><see cref="GGPOErrorCode"/> type of error.</param>
        public GGPOException(GGPOErrorCode code)
            : base($"GGPO Error: {code}")
        {
            ErrorCode = code;
        }
    }
}
