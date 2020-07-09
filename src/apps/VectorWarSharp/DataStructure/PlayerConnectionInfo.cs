using GGPOSharp;

namespace VectorWar.DataStructure
{
    struct PlayerConnectionInfo
    {
        public GGPOPlayerType type;
        public int playerHandle;
        public PlayerConnectState state;
        public int connectProgress;
        public int disconnectTimeout;
        public int disconnectStart;
    }
}
