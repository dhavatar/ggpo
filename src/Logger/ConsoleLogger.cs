using GGPOSharp.Interfaces;
using System;

namespace GGPOSharp.Logger
{
    /// <summary>
    /// Simple console writing implementation of ILog.
    /// </summary>
    public class ConsoleLogger : ILog
    {
        /// <summary>
        /// Static method to create a logger for a class.
        /// </summary>
        /// <returns><see cref="ConsoleLogger"/></returns>
        public static ConsoleLogger GetLogger()
        {
            return new ConsoleLogger();
        }

        /// <summary>
        /// Outputs the string into the console.
        /// </summary>
        /// <param name="msg">String to output to the console.</param>
        public void Log(string msg)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss:fff")}]: {msg}");
        }
    }
}
