namespace GGPOSharp.Interfaces
{
    /// <summary>
    /// Interface contains the callback functions that the application must implement.
    /// GGPO.net will periodically call these functions during the game.
    /// </summary>
    public interface IGGPOSessionCallbacks
    {
        /// <summary>
        /// The client should allocate a buffer and copy the entire contents of the current game
        /// state into it. Optionally, the client can compute a checksum of the data and store
        /// it in the checksum.
        /// </summary>
        /// <param name="frame">Current <see cref="Sync.SavedFrame"/> state of the game.</param>
        /// <returns></returns>
        bool SaveGameState(ref Sync.SavedFrame frame);

        /// <summary>
        /// GGPO.net will call this function at the beginning of a rollback. The buffer and
        /// len parameters contain a previously saved state returned from the SaveGameState
        /// function. The client should make the current game state match the state contained
        /// in the buffer.
        /// </summary>
        /// <param name="buffer">Game state to load into the game.</param>
        /// <returns></returns>
        bool LoadGameState(byte[] buffer);

        /// <summary>
        /// Used in diagnostic testing. The client should use the log function to write the
        /// contents of the specified save state in a human readable form.
        /// </summary>
        /// <param name="filename">Name of the file to store the game state information.</param>
        /// <param name="buffer">Game state buffer to log.</param>
        /// <returns></returns>
        bool LogGameState(string filename, byte[] buffer);

        /// <summary>
        /// Called during a rollback. You should advance your game
        /// state by exactly one frame. Before each frame, call SynchronizeInput
        /// to retrieve the inputs you should use for that frame. After each frame,
        /// you should call AdvanceFrame to notify GGPO.net that you're finished.
        /// </summary>
        /// <returns></returns>
        bool AdvanceFrame();

        /// <summary>
        /// Callback when the handshake with the game running on the other side of the network
        /// has been completed.
        /// </summary>
        /// <param name="playerId"></param>
        void OnConnected(int playerId);

        /// <summary>
        /// Callback for beginning the synchronization process with the client on the other end of the
        /// networking. The count and total parameters indicate progress.
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="count"></param>
        /// <param name="total"></param>
        void OnSynchronizing(int playerId, int count, int total);

        /// <summary>
        /// Callback when the synchronziation with this peer has finished.
        /// </summary>
        /// <param name="playerId"></param>
        void OnSyncrhonized(int playerId);

        /// <summary>
        /// Callback when the network connection on the other end of the network has closed.
        /// </summary>
        /// <param name="playerId"></param>
        void OnDisconnected(int playerId);

        /// <summary>
        /// Callback when the time synchronziation code has determined that this client is too
        /// far ahead of the other one and should slow down to ensure fairness.
        /// </summary>
        /// <param name="framesAhead">How many frames ahead the client is.</param>
        void OnTimeSync(int framesAhead);

        /// <summary>
        /// Callback when the connection to the player has been interrupted.
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="disconnectTimeout"></param>
        void OnConnectionInterrupted(int playerId, int disconnectTimeout);

        /// <summary>
        /// Callback when the connection to the player has been restored.
        /// </summary>
        /// <param name="playerId"></param>
        void OnConnectionResumed(int playerId);
    }
}
