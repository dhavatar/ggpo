using GGPOSharp.Interfaces;
using GGPOSharp.Network;

namespace GGPOSharp
{
    /// <summary>
    /// Base class used to define a GGPO session.
    /// </summary>
    public abstract class GGPOSession
    {
        /// <summary>
        /// Must be called for each player in the session (e.g. in a 3 player session, must
        /// be called 3 times).
        /// </summary>
        /// <param name="player">A <see cref="GGPOPlayer"/> used to describe the player.</param>
        /// <param name="playerHandle">An out parameter to a handle used to identify this player in the future.</param>
        /// <returns><see cref="GGPOErrorCode"/> result of the operation.</returns>
        public abstract GGPOErrorCode AddPlayer(GGPOPlayer player, out int playerHandle);

        /// <summary>
        /// Used to notify GGPO.net of inputs that should be trasmitted to remote
        /// players. AddLocalInput must be called once every frame for all local players.
        /// </summary>
        /// <param name="playerHandle">The player handle returned for this player when you called
        /// AddLocalPlayer.</param>
        /// <param name="values">The controller inputs for this player. The length must be
        /// exactly equal to the input length passd into StartSession.</param>
        /// <returns><see cref="GGPOErrorCode"/> result of the operation.</returns>
        public abstract GGPOErrorCode AddLocalInput(int playerHandle, byte[] values);

        /// <summary>
        /// You should call SyncInput before every frame of execution, including those frames which
        /// happen during rollback.
        /// </summary>
        /// <param name="values">When the function returns, the values parameter will contain
        /// inputs for this frame for all players. The values array must be at least
        /// (size * players) large.</param>
        /// <param name="disconnectFlags">Indicated whether the input in slot (1 << flag) is
        /// valid. If a player has disconnected, the input in the values array for
        /// that player will be zeroed and the i-th flag will be set. For example,
        /// if only player 3 has disconnected, disconnect flags will be 8 (i.e. 1 << 3).</param>
        /// <returns><see cref="GGPOErrorCode"/> result of the operation.</returns>
        public abstract GGPOErrorCode SyncInput(byte[] values, int disconnectFlags);

        /// <summary>
        /// Should be called periodically by your application to give GGPO.net
        /// a chance to do some work. Most packet transmissions and rollbacks occur
        /// in Idle.
        /// </summary>
        /// <param name="timeout">The amount of time GGPO.net is allowed to spend in this function,
        /// in milliseconds.</param>
        /// <returns><see cref="GGPOErrorCode"/> result of the operation.</returns>
        public virtual GGPOErrorCode Idle(int timeout)
        {
            return GGPOErrorCode.OK;
        }

        /// <summary>
        /// You should call AdvanceFrame to notify GGPO.net that you have
        /// advanced your gamestate by a single frame. You should call this everytime
        /// you advance the gamestate by a frame, even during rollbacks. GGPO.net
        /// may call your SaveState callback before this function returns.
        /// </summary>
        /// <returns><see cref="GGPOErrorCode"/> result of the operation.</returns>
        public virtual GGPOErrorCode AdvanceFrame()
        {
            return GGPOErrorCode.OK;
        }

        public virtual GGPOErrorCode Chat(string text)
        {
            return GGPOErrorCode.OK;
        }

        /// <summary>
        /// Disconnects a remote player from a game. Will return <see cref="GGPOErrorCode.PlayerDisconnected"/>
        /// if you try to disconnect a player who has already been disconnected.
        /// </summary>
        /// <param name="playerHandle">The player handle returned for this player when you called
        /// AddLocalPlayer.</param>
        /// <returns><see cref="GGPOErrorCode"/> result of the operation.</returns>
        public virtual GGPOErrorCode DisconnectPlayer(int playerHandle)
        { 
            return GGPOErrorCode.OK;
        }

        /// <summary>
        /// Used to fetch some statistics about the quality of the network connection.
        /// </summary>
        /// <param name="playerHandle">The player handle returned from the AddPlayer function you used
        /// to add the remote player.</param>
        /// <param name="stats">Out parameter to the network statistics.</param>
        /// <returns><see cref="GGPOErrorCode"/> result of the operation.</returns>
        public virtual GGPOErrorCode GetNetworkStats(int playerHandle, out GGPONetworkStats stats) 
        {
            stats = null;
            return GGPOErrorCode.OK;
        }

        /// <summary>
        /// Change the amount of frames ggpo will delay local input. Must be called
        /// before the first call to SynchronizeInput.
        /// </summary>
        /// <param name="playerHandle">Integer player handle to apply the delay.</param>
        /// <param name="delay">Amount of frames to delay.</param>
        /// <returns><see cref="GGPOErrorCode"/> on the result of the operation.</returns>
        public virtual GGPOErrorCode SetFrameDelay(int playerHandle, int delay)
        {
            return GGPOErrorCode.Unsupported;
        }

        /// <summary>
        /// Sets the disconnect timeout. The session will automatically disconnect
        /// from a remote peer if it has not received a packet in the timeout window.
        /// You will be notified of the disconnect via a <see cref="IGGPOSessionCallbacks.OnDisconnected(int)"/>
        /// callback.
        /// 
        /// Setting a timeout value of 0 will disable automatic disconnects.
        /// </summary>
        /// <param name="timeout">The time in milliseconds to wait before disconnecting a peer.</param>
        /// <returns><see cref="GGPOErrorCode"/> on the result of the operation.</returns>
        public virtual GGPOErrorCode SetDisconnectTimeout(int timeout)
        {
            return GGPOErrorCode.Unsupported;
        }

        /// <summary>
        /// The time to wait before the first <see cref="IGGPOSessionCallbacks.OnConnectionInterrupted(int, int)"/>
        /// callback will be invoked.
        /// </summary>
        /// <param name="timeout">The amount of time which needs to elapse without receiving a packet
        /// before the <see cref="IGGPOSessionCallbacks.OnConnectionInterrupted(int, int)"/>
        /// callback is invoked.</param>
        /// <returns><see cref="GGPOErrorCode"/> on the result of the operation.</returns>
        public virtual GGPOErrorCode SetDisconnectNotifyStart(int timeout)
        {
            return GGPOErrorCode.Unsupported;
        }

        /// <summary>
        /// Used to write to the ggpo.net log.
        /// </summary>
        /// <param name="logger"><see cref="ILog"/> type to use for outputing the log message.</param>
        /// <param name="msg">Log message to output.</param>
        /// <returns><see cref="GGPOErrorCode"/> on the result of the operation.</returns>
        public virtual GGPOErrorCode Log(ILog logger, string msg)
        {
            logger?.Log(msg);
            return GGPOErrorCode.OK;
        }
    }
}
