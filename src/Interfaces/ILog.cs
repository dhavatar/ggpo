namespace GGPOSharp.Interfaces
{
    /// <summary>
    /// Generic logging interface for the library.
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="msg">String message to output.</param>
        void Log(string msg);
    }
}
