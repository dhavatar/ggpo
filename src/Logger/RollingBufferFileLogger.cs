using GGPOSharp.Interfaces;
using System;
using System.IO;

namespace GGPOSharp.Logger
{
    /// <summary>
    /// Rolling buffer logger where it'll store all the logs in a buffer and then write all of it out
    /// to a file when the program ends in order not to disrupt the program with IO operations.
    /// </summary>
    public class RollingBufferFileLogger : ILog
    {
        /// <summary>
        /// Buffer for holding the logs
        /// </summary>
        private string[] msgs;

        /// <summary>
        /// Buffer index for the next msg.
        /// </summary>
        private int index = 0;

        /// <summary>
        /// Name of the log file where it will be written.
        /// </summary>
        private string filename;

        /// <summary>
        /// Constructor that initializes the filename for the log file.
        /// </summary>
        /// <param name="filename">File name for the log file.</param>
        public RollingBufferFileLogger(string filename, int size = 10000)
        {
            this.filename = filename;
            msgs = new string[size];
        }

        /// <summary>
        /// On destruction, write out all the contents of the buffer.
        /// </summary>
        ~RollingBufferFileLogger()
        {
            string path = $"{DateTime.Now:HH.mm.ss}-{filename}.txt";
            File.WriteAllLines(path, msgs);
        }

        /// <summary>
        /// Outputs the string into the console.
        /// </summary>
        /// <param name="msg">String to output to the console.</param>
        public void Log(string msg)
        {
            msgs[index] = $"[{DateTime.Now:HH:mm:ss:fff}]: {msg}";
            index = (index + 1) % msgs.Length;
        }
    }
}
