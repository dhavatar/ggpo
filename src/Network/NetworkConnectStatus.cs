﻿using System;

namespace GGPOSharp.Network
{
    [Serializable]
    public class NetworkConnectStatus
    {
        private int data;

        public bool Disconnected
        {
            get => (data & 1) != 0;
            set => data = (data & ~1) | (value ? 1 : 0);
        }

        public int LastFrame
        {
            get => data >> 1;
            set => data = (data & 1) | (value << 1);
        }

        /// <summary>
        /// Default constructor sets the last frame as <see cref="GameInput.NullFrame"/>.
        /// </summary>
        public NetworkConnectStatus()
        {
            LastFrame = GameInput.NullFrame;
            Disconnected = false;
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
