using System;

namespace GGPOSharp
{
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
