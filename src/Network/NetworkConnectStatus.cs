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
    }
}
