namespace GGPOSharp
{
    public enum GGPOPlayerType
    {
        Local,
        Remote,
        Spectator,
    }

    public struct GGPOLocalEndpoint
    {
        public int playerNum;
    }

    /// <summary>
    /// The structure used to describe players in <see cref="GGPOSession.AddPlayer(GGPOPlayer, out int)"/>
    /// </summary>
    public struct GGPOPlayer
    {
        /// <summary>
        /// One of the GGPOPlayerType values describing how inputs should be handled.
        /// Local players must have their inputs updated every frame via
        /// ggpo_add_local_inputs. Remote players values will come over the
        /// network.
        /// </summary>
        public GGPOPlayerType type;

        /// <summary>
        /// The player number. Should be between 1 and the number of players
        /// in the game(e.g. in a 2 player game, either 1 or 2).
        /// </summary>
        public int playerId;

        /// <summary>
        /// The ip address of the ggpo session which will host this player.
        /// </summary>
        public string ipAddress;

        /// <summary>
        /// The port where udp packets should be sent to reach this player.
        /// All the local inputs for this session will be sent to this player at
        /// ipAddress:port.
        /// </summary>
        public int port;
    }
}
