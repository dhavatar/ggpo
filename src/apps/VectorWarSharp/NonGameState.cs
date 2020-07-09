namespace VectorWar
{
    /// <summary>
    /// These are other pieces of information not related to the state
    /// of the game which are useful to carry around.They are not
    /// included in the GameState class because they specifically
    /// should not be rolled back.
    /// </summary>
    class NonGameState
    {
        public struct ChecksumInfo
        {
            public int frameNumber;
            public int checksum;
        }

        public int LocalPlayerHandle { get; set; }
        public DataStructure.PlayerConnectionInfo[] players { get; set; } = new DataStructure.PlayerConnectionInfo[Constants.MaxPlayers];
        public int NumPlayers { get; set; }

        public ChecksumInfo ChecksumNow { get; set; }
        public ChecksumInfo ChecksumPeriodic { get; set; }

        public void SetConnectState(int playerHandle, PlayerConnectState state)
        {
            for (int i = 0; i < NumPlayers; i++)
            {
                if (players[i].playerHandle == playerHandle)
                {
                    players[i].connectProgress = 0;
                    players[i].state = state;
                    break;
                }
            }
        }

        public void SetDisconnectTimeout(int playerHandle, int when, int timeout)
        {
            for (int i = 0; i < NumPlayers; i++)
            {
                if (players[i].playerHandle == playerHandle)
                {
                    players[i].disconnectStart = when;
                    players[i].disconnectTimeout = timeout;
                    players[i].state = PlayerConnectState.Disconnecting;
                    break;
                }
            }
        }

        public void SetConnectState(PlayerConnectState state)
        {
            for (int i = 0; i < NumPlayers; i++)
            {
                players[i].state = state;
            }
        }

        public void UpdateConnectProgress(int playerHandle, int progress)
        {
            for (int i = 0; i < NumPlayers; i++)
            {
                if (players[i].playerHandle == playerHandle)
                {
                    players[i].connectProgress = progress;
                    break;
                }
            }
        }
    }
}
