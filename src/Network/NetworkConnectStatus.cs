using System;

namespace GGPOSharp.Network
{
    [Serializable]
    public class NetworkConnectStatus
    {
        private uint data;

        public bool Disconnected
        {
            get => (data & 1) != 0;
            set => data = (data & ~1u) | (value ? 1u : 0u);
        }

        public int LastFrame
        {
            get => (int)(data << 1);
            set => data = (data & 1) | (uint)(value << 1);
        }

        /// <summary>
        /// Default constructor sets the last frame as <see cref="GameInput.NullFrame"/>.
        /// </summary>
        public NetworkConnectStatus()
        {
            LastFrame = GameInput.NullFrame;
        }

        /// <summary>
        /// Helper method to copy data from another object.
        /// </summary>
        /// <param name="other"><see cref="NetworkConnectStatus"/> to copy into this class.</param>
        public void Copy(NetworkConnectStatus other)
        {
            Disconnected = other.Disconnected;
            LastFrame = other.LastFrame;
        }

        /// <summary>
        /// Resets the status values to disconnected = false and last frame = 0.
        /// </summary>
        public void Reset()
        {
            Disconnected = false;
            LastFrame = 0;
        }
    }
}
