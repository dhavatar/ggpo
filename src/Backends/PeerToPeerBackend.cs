using GGPOSharp.Interfaces;
using GGPOSharp.Network;

namespace GGPOSharp.Backends
{
    public class PeerToPeerBackend : GGPOSession
    {
        public const int RecommendationInterval = 240;
        public const int DefaultDisconnectTimeout = 5000;
        public const int DefaultDisconnectNotifyStart = 750;

        protected Sync sync;
        protected int numSpectators = 0;
        protected int inputSize;
        protected bool isSynchronizing;
        protected int nextRecommendedSleep;
        protected int nextSpectatorFrame = 0;
        protected int disconnectTimeout = DefaultDisconnectTimeout;
        protected int disconnectNotifyStart = DefaultDisconnectNotifyStart;
        protected NetworkConnectStatus[] localConnectStatus;
        protected UdpProtocol[] endpoints;
        protected UdpProtocol[] spectators = new UdpProtocol[Constants.MaxSpectators];

        public PeerToPeerBackend(IGGPOSessionCallbacks callbacks, ILog logger, int localPort, int numPlayers, int inputSize)
        {
            this.numPlayers = numPlayers;
            this.inputSize = inputSize;
            this.callbacks = callbacks;
            this.logger = logger;

            isSynchronizing = true;
            endpoints = new UdpProtocol[numPlayers];

            // Initialize the synchronziation layer
            sync = new Sync(new Sync.Config
            {
                numPlayers = numPlayers,
                inputSize = inputSize,
                callbacks = callbacks,
                numPredictionFrames = Constants.MaxPredictionFrames,
            });

            // Initialize the UDP port
            // TODO
        }

        public override GGPOErrorCode AddLocalInput(int playerHandle, byte[] values)
        {
            GGPOErrorCode result;

            if (sync.InRollback)
            {
                return GGPOErrorCode.InRollback;
            }
            if (isSynchronizing)
            {
                return GGPOErrorCode.NotSynchronized;
            }

            result = PlayerHandleToQueue(playerHandle, out int queue);
            if (result != GGPOErrorCode.Success)
            {
                return result;
            }

            var input = new GameInput(GameInput.NullFrame, values, (uint)values.Length);

            // Feed the input for the current frame into the synchronzation layer.
            if (!sync.AddLocalInput(queue, ref input))
            {
                return GGPOErrorCode.PredictionThreshold;
            }

            if (input.frame != GameInput.NullFrame)
            {
                // xxx: <- comment why this is the case
                // Update the local connect status state to indicate that we've got a
                // confirmed local frame for this player.  this must come first so it
                // gets incorporated into the next packet we send.

                Log($"setting local connect status for local queue {queue} to {input.frame}");
                localConnectStatus[queue].LastFrame = input.frame;

                // Send the input to all the remote players.
                for (int i = 0; i < numPlayers; i++)
                {
                    if (endpoints[i].IsInitialized)
                    {
                        endpoints[i].SendInput(ref input);
                    }
                }
            }

            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode AddPlayer(GGPOPlayer player, out int playerHandle)
        {
            playerHandle = 0;

            if (player.type == GGPOPlayerType.Spectator)
            {
                return AddSpectator(player.ipAddress, player.port);
            }

            int queue = player.playerId - 1;
            if (player.playerId < 1 || player.playerId > numPlayers)
            {
                return GGPOErrorCode.PlayerOutOfRange;
            }

            playerHandle = QueueToPlayerHandle(queue);

            if (player.type == GGPOPlayerType.Remote)
            {
                AddRemotePlayer(player.ipAddress, player.port, queue);
            }

            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode SyncInput(byte[] values, ref int disconnectFlags)
        {
            if (!isSynchronizing)
            {
                disconnectFlags = 0;
                return GGPOErrorCode.NotSynchronized;
            }

            disconnectFlags = sync.SyncrhonizeInputs(values);
            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode AdvanceFrame()
        {
            Log($"End of frame ({sync.FrameCount})...");
            sync.IncrementFrame();

            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode DisconnectPlayer(int playerHandle)
        {
            GGPOErrorCode result;

            result = PlayerHandleToQueue(playerHandle, out int queue);
            if (result != GGPOErrorCode.Success)
            {
                return result;
            }

            if (localConnectStatus[queue].Disconnected)
            {
                return GGPOErrorCode.PlayerDisconnected;
            }

            if (!endpoints[queue].IsInitialized)
            {
                // xxx: we should be tracking who the local player is, but for now assume
                // that if the endpoint is not initalized, this must be the local player.
                Log($"Disconnecting local player {queue} at frame {localConnectStatus[queue].LastFrame} by user request.");
                for (int i = 0; i < numPlayers; i++)
                {
                    if (endpoints[i].IsInitialized)
                    {
                        DisconnectPlayerQueue(i, sync.FrameCount);
                    }
                }
            }
            else
            {
                Log($"Disconnecting queue {queue} at frame {localConnectStatus[queue].LastFrame} by user request.");
                DisconnectPlayerQueue(queue, localConnectStatus[queue].LastFrame);
            }
            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode GetNetworkStats(int playerHandle, out GGPONetworkStats stats)
        {
            stats = null;

            GGPOErrorCode result = PlayerHandleToQueue(playerHandle, out int queue);
            if (result != GGPOErrorCode.Success)
            {
                return result;
            }

            endpoints[queue].GetNetworkStats(out stats);
            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode SetFrameDelay(int playerHandle, int delay)
        {
            GGPOErrorCode result = PlayerHandleToQueue(playerHandle, out int queue);
            if (result != GGPOErrorCode.Success)
            {
                return result;
            }

            sync.SetFrameDelay(queue, delay);
            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode SetDisconnectTimeout(int timeout)
        {
            disconnectTimeout = timeout;
            for (int i = 0; i < numPlayers; i++)
            {
                if (endpoints[i].IsInitialized)
                {
                    endpoints[i].DisconnectTimeout = disconnectTimeout;
                }
            }
            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode SetDisconnectNotifyStart(int timeout)
        {
            disconnectNotifyStart = timeout;
            for (int i = 0; i < numPlayers; i++)
            {
                if (endpoints[i].IsInitialized)
                {
                    endpoints[i].DisconnectNotifyStart = disconnectNotifyStart;
                }
            }
            return GGPOErrorCode.OK;
        }

        protected void AddRemotePlayer(string ip, int port, int queue)
        {
            isSynchronizing = true;

            endpoints[queue] = new UdpProtocol(queue, ip, port, localConnectStatus, logger);
            endpoints[queue].DisconnectTimeout = disconnectTimeout;
            endpoints[queue].DisconnectNotifyStart = disconnectNotifyStart;
            endpoints[queue].Synchronize();
        }

        protected GGPOErrorCode AddSpectator(string ip, int port)
        {
            if (numSpectators == Constants.MaxSpectators)
            {
                return GGPOErrorCode.TooManySpectators;
            }

            // Currently, we can only add spectators before the game starts.
            if (isSynchronizing)
            {
                return GGPOErrorCode.InvalidRequest;
            }
            int queue = numSpectators++;

            spectators[queue] = new UdpProtocol(queue + 1000, ip, port, localConnectStatus, logger);
            spectators[queue].DisconnectTimeout = disconnectTimeout;
            spectators[queue].DisconnectNotifyStart = disconnectNotifyStart;
            spectators[queue].Synchronize();

            return GGPOErrorCode.OK;
        }

        protected void DisconnectPlayerQueue(int queue, int syncTo)
        {
            endpoints[queue].Disconnect();

            Log($"Changing queue {queue} local connect status for last frame from {localConnectStatus[queue].LastFrame} to {syncTo} on disconnect request (current: {sync.FrameCount}).");

            localConnectStatus[queue].Disconnected = true;
            localConnectStatus[queue].LastFrame = syncTo;

            if (syncTo < sync.FrameCount)
            {
                Log($"adjusting simulation to account for the fact that {queue} disconnected @ {syncTo}.");
                sync.AdjustSimulation(syncTo);
                Log("finished adjusting simulation.");
            }

            callbacks.OnDisconnected(QueueToPlayerHandle(queue));
            CheckInitialSync();
        }

        protected GGPOErrorCode PlayerHandleToQueue(int playerHandle, out int queue)
        {
            int offset = playerHandle - 1;
            queue = -1;

            if (offset < 0 || offset >= numPlayers)
            {
                return GGPOErrorCode.InvalidPlayerHandle;
            }
            
            queue = offset;
            return GGPOErrorCode.OK;
        }

        protected void CheckInitialSync()
        {
            if (isSynchronizing)
            {
                // Check to see if everyone is now synchronized.  If so,
                // go ahead and tell the client that we're ok to accept input.
                for (int i = 0; i < numPlayers; i++)
                {
                    // xxx: IsInitialized() must go... we're actually using it as a proxy for "represents the local player"
                    if (endpoints[i].IsInitialized && !endpoints[i].IsSynchronized && !localConnectStatus[i].Disconnected)
                    {
                        return;
                    }
                }

                for (int i = 0; i < numSpectators; i++)
                {
                    if (spectators[i].IsInitialized && !spectators[i].IsSynchronized)
                    {
                        return;
                    }
                }

                callbacks.OnRunning();
                isSynchronizing = false;
            }
        }

        protected int QueueToPlayerHandle(int queue)
        {
            return queue + 1;
        }

        protected int QueueToSpectatorHandle(int queue)
        {
            return queue + 1000;
        }
    }
}
