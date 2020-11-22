using GGPOSharp.Interfaces;

namespace GGPOSharp
{
    /// <summary>
    /// Base class that contains the implementation for using the logger to record debugging information.
    /// </summary>
    public class BaseLogging
    {
        /// <summary>
        /// Logger used for recording information.
        /// </summary>
        private ILog Logger { get; set; }

        /// <summary>
        /// Constructor that will initialize the logging.
        /// </summary>
        /// <param name="logger"><see cref="ILog"/> that will perform the logging.</param>
        public BaseLogging(ILog logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Logs the passed in message.
        /// </summary>
        /// <param name="msg">String message to log.</param>
        public virtual void Log(string msg)
        {
            if (Logger == null)
            {
                return;
            }

            Logger.Log(msg);
        }
    }
}
