using GGPOSharp.Interfaces;
using GGPOSharp.Network;
using GGPOSharp.Network.Events;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GGPOSharp.Backends
{
    public class PeerToPeerBackend : GGPOSession
    {
        public const int SIO_UDP_CONNRESET = -1744830452;
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
        protected NetworkConnectStatus[] localConnectStatus = new NetworkConnectStatus[Constants.MaxPlayers];
        protected UdpProtocol[] endpoints;
        protected UdpProtocol[] spectators = new UdpProtocol[Constants.MaxSpectators];
        protected UdpClient udp;

        private Poll poll = new Poll();

        public PeerToPeerBackend(IGGPOSessionCallbacks callbacks, ILog logger, int localPort, int numPlayers, int inputSize)
        {
            this.numPlayers = numPlayers;
            this.inputSize = inputSize;
            this.callbacks = callbacks;
            this.logger = logger;

            for (int i = 0; i < Constants.MaxPlayers; i++)
            {
                localConnectStatus[i] = new NetworkConnectStatus();
            }

            for (int i = 0; i < Constants.MaxSpectators; i++)
            {
                spectators[i] = new UdpProtocol(logger);
            }

            isSynchronizing = true;
            endpoints = new UdpProtocol[numPlayers];
            for (int i = 0; i < numPlayers; i++)
            {
                endpoints[i] = new UdpProtocol(logger);
            }

            // Initialize the synchronziation layer
            sync = new Sync(localConnectStatus,
                new Sync.Config
                {
                    numPlayers = numPlayers,
                    inputSize = inputSize,
                    callbacks = callbacks,
                    numPredictionFrames = Constants.MaxPredictionFrames,
                });

            // Initialize the UDP port
            var udpEndpoint = new IPEndPoint(IPAddress.Any, localPort);
            udp = new UdpClient(udpEndpoint);

            // Ignore the connect reset message in Windows to prevent a UDP shutdown exception
            udp.Client.IOControl(
                (IOControlCode)SIO_UDP_CONNRESET,
                new byte[] { 0, 0, 0, 0 },
                null
            );
            udp.BeginReceive(new System.AsyncCallback(OnMessage), udpEndpoint);
        }

        public void OnMessage(IAsyncResult res)
        {
            var endpoint = (IPEndPoint)res.AsyncState;

            byte[] receiveBytes = udp.EndReceive(res, ref endpoint);
            udp.BeginReceive(new System.AsyncCallback(OnMessage), endpoint);

            var msg = Utility.Deserialize<NetworkMessage>(receiveBytes);

            for (int i = 0; i < numPlayers; i++)
            {
                if (endpoints[i].HandlesMessage(endpoint, msg))
                {
                    endpoints[i].OnMessage(msg);
                    return;
                }
            }

            for (int i = 0; i < numSpectators; i++)
            {
                if (spectators[i].HandlesMessage(endpoint, msg))
                {
                    spectators[i].OnMessage(msg);
                    return;
                }
            }
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

        public override GGPOErrorCode Idle(int timeout)
        {
            if (sync.InRollback)
            {
                return GGPOErrorCode.OK;
            }

            poll.Pump(0);
            PollUdpProtocolEvents();

            if (isSynchronizing)
            {
                return GGPOErrorCode.OK;
            }

            sync.CheckSimulation(timeout);

            // notify all of our endpoints of their local frame number for their
            // next connection quality report
            for (int i = 0; i < numPlayers; i++)
            {
                endpoints[i].SetLocalFrameNumber(sync.FrameCount);
            }

            int lastConfirmedFrame;
            if (numPlayers <= 2)
            {
                lastConfirmedFrame = Poll2Players(sync.FrameCount);
            }
            else
            {
                lastConfirmedFrame = PollNPlayers(sync.FrameCount);
            }

            Log($"last confirmed frame in p2p backend is {lastConfirmedFrame}.");

            if (lastConfirmedFrame >= 0)
            {
                Debug.Assert(lastConfirmedFrame != int.MaxValue);
                if (numSpectators > 0)
                {
                    while (nextSpectatorFrame <= lastConfirmedFrame)
                    {
                        Log($"pushing frame {nextSpectatorFrame} to spectators.");

                        var input = new GameInput
                        {
                            frame = nextSpectatorFrame,
                            size = (uint)(inputSize * numPlayers),
                        };
                        sync.GetConfirmedInputs(input.bits, nextSpectatorFrame);

                        for (int i = 0; i < numSpectators; i++)
                        {
                            spectators[i].SendInput(ref input);
                        }
                        nextSpectatorFrame++;
                    }
                }

                Log($"setting confirmed frame in sync to {lastConfirmedFrame}.");
                sync.SetLastConfirmedFrame(lastConfirmedFrame);
            }

            if (timeout > 0)
            {
                Thread.Sleep(1);
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

        public override GGPOErrorCode SyncInput(ref byte[] values, ref int disconnectFlags)
        {
            if (!isSynchronizing)
            {
                disconnectFlags = 0;
                return GGPOErrorCode.NotSynchronized;
            }

            disconnectFlags = sync.SyncrhonizeInputs(ref values);
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

            endpoints[queue] = new UdpProtocol(udp, poll, queue, ip, port, localConnectStatus, logger);
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

            spectators[queue] = new UdpProtocol(udp, poll, queue + 1000, ip, port, localConnectStatus, logger);
            spectators[queue].DisconnectTimeout = disconnectTimeout;
            spectators[queue].DisconnectNotifyStart = disconnectNotifyStart;
            spectators[queue].Synchronize();

            return GGPOErrorCode.OK;
        }

        protected int Poll2Players(int currentFrame)
        {
            // discard confirmed frames as appropriate
            int totalMinConfirmed = int.MaxValue;

            for (int i = 0; i < numPlayers; i++)
            {
                var queueConnected = true;
                if (endpoints[i].IsRunning)
                {
                    queueConnected = endpoints[i].GetPeerConnectStatus(i, out int frame);
                }

                if (!localConnectStatus[i].Disconnected)
                {
                    totalMinConfirmed = Math.Min(localConnectStatus[i].LastFrame, totalMinConfirmed);
                }

                //Log($"  local endp: connected = {!localConnectStatus[i].Disconnected}, last_received = {localConnectStatus[i].LastFrame}, total_min_confirmed = {totalMinConfirmed}.");
                if (!queueConnected && !localConnectStatus[i].Disconnected)
                {
                    Log($"disconnecting i {i} by remote request.");
                    DisconnectPlayerQueue(i, totalMinConfirmed);
                }

                //Log($"  total_min_confirmed = {totalMinConfirmed}.");
            }

            return totalMinConfirmed;
        }

        protected int PollNPlayers(int currentFrame)
        {
            // discard confirmed frames as appropriate
            int totalMinConfirmed = int.MaxValue;

            for (int queue = 0; queue < numPlayers; queue++)
            {
                var queueConnected = true;
                var queueMinConfirmed = int.MaxValue;
                Log($"considering queue {queue}.");
                
                for (int i = 0; i < numPlayers; i++)
                {
                    // we're going to do a lot of logic here in consideration of endpoint i.
                    // keep accumulating the minimum confirmed point for all n*n packets and
                    // throw away the rest.
                    if (endpoints[i].IsRunning)
                    {
                        bool connected = endpoints[i].GetPeerConnectStatus(queue, out int lastReceived);

                        queueConnected = queueConnected && connected;
                        queueMinConfirmed = Math.Min(lastReceived, queueMinConfirmed);
                        Log($"  endpoint {i}: connected = {connected}, last_received = {lastReceived}, queue_min_confirmed = {queueMinConfirmed}.");
                    }
                    else
                    {
                        Log($"  endpoint {i}: ignoring... not running.");
                    }
                }

                // merge in our local status only if we're still connected!
                if (!localConnectStatus[queue].Disconnected)
                {
                    queueMinConfirmed = Math.Min(localConnectStatus[queue].LastFrame, queueMinConfirmed);
                }
                Log($"  local endp: connected = {!localConnectStatus[queue].Disconnected}, last_received = {localConnectStatus[queue].LastFrame}, queue_min_confirmed = {queueMinConfirmed}.");

                if (queueConnected)
                {
                    totalMinConfirmed = Math.Min(queueMinConfirmed, totalMinConfirmed);
                }
                else
                {
                    // check to see if this disconnect notification is further back than we've been before. If
                    // so, we need to re-adjust. This can happen when we detect our own disconnect at frame n
                    // and later receive a disconnect notification for frame n-1.
                    if (!localConnectStatus[queue].Disconnected || localConnectStatus[queue].LastFrame > queueMinConfirmed)
                    {
                        Log($"disconnecting queue {queue} by remote request.");
                        DisconnectPlayerQueue(queue, queueMinConfirmed);
                    }
                }
                Log($"  total_min_confirmed = {totalMinConfirmed}.");
            }

            return totalMinConfirmed;
        }

        protected void PollUdpProtocolEvents()
        {
            for (int i = 0; i < numPlayers; i++)
            {
                while (endpoints[i].GetEvent(out UdpProtocolEvent evt))
                {
                    OnUdpProtocolPeerEvent(evt, i);
                }
            }

            for (int i = 0; i < numSpectators; i++)
            {
                while (spectators[i].GetEvent(out UdpProtocolEvent evt))
                {
                    OnUdpProtocolSpectatorEvent(evt, i);
                }
            }
        }

        protected void OnUdpProtocolPeerEvent(UdpProtocolEvent evt, int queue)
        {
            OnUdpProtocolEvent(evt, QueueToPlayerHandle(queue));

            switch (evt.EventType)
            {
                case UdpProtocolEvent.Type.Input:
                    var inputEvt = evt as InputEvent;
                    if (!localConnectStatus[queue].Disconnected)
                    {
                        int currentRemoteFrame = localConnectStatus[queue].LastFrame;
                        int newRemoteFrame = inputEvt.Input.frame;
                        Log($"currentRemoteFrame: {currentRemoteFrame}  newRemoteFrame: {newRemoteFrame}");
                        Debug.Assert(currentRemoteFrame == -1 || newRemoteFrame == (currentRemoteFrame + 1));

                        GameInput input = inputEvt.Input;
                        sync.AddRemoteInput(queue, ref input);

                        // Notify the other endpoints which frame we received from a peer
                        Log($"setting remote connect status for queue {queue} to {input.frame}");
                        localConnectStatus[queue].LastFrame = input.frame;
                    }
                    break;

                case UdpProtocolEvent.Type.Disconnected:
                    DisconnectPlayer(QueueToPlayerHandle(queue));
                    break;
            }
        }

        protected void OnUdpProtocolSpectatorEvent(UdpProtocolEvent evt, int queue)
        {
            int handle = QueueToSpectatorHandle(queue);
            OnUdpProtocolPeerEvent(evt, handle);

            if (evt.EventType == UdpProtocolEvent.Type.Disconnected)
            {
                spectators[queue].Disconnect();
                callbacks.OnDisconnected(handle);
            }
        }

        protected void OnUdpProtocolEvent(UdpProtocolEvent evt, int handle)
        {
            switch (evt.EventType)
            {
                case UdpProtocolEvent.Type.Connected:
                    callbacks.OnConnected(handle);
                    break;

                case UdpProtocolEvent.Type.Synchronizing:
                    var syncEvent = evt as SynchronizingEvent;
                    callbacks.OnSynchronizing(handle, syncEvent.Count, syncEvent.Total);
                    break;

                case UdpProtocolEvent.Type.Synchronized:
                    callbacks.OnSyncrhonized(handle);
                    CheckInitialSync();
                    break;

                case UdpProtocolEvent.Type.NetworkInterrupted:
                    var netEvent = evt as NetworkInterruptedEvent;
                    callbacks.OnConnectionInterrupted(handle, netEvent.DisconnectTimeout);
                    break;

                case UdpProtocolEvent.Type.NetworkResumed:
                    callbacks.OnConnectionResumed(handle);
                    break;
            }
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
