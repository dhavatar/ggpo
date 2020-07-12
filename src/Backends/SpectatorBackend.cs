using GGPOSharp.Interfaces;
using GGPOSharp.Network;
using GGPOSharp.Network.Events;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace GGPOSharp.Backends
{
    public class SpectatorBackend : GGPOSession
    {
        const int SpectatorFrameBufferSize = 64;

        protected UdpClient udp;
        protected UdpProtocol host;
        protected bool isSynchronizing = true;
        protected int inputSize;
        protected int nextInputToSend = 0;
        protected GameInput[] inputs = new GameInput[SpectatorFrameBufferSize];

        private Poll poll = new Poll();

        public SpectatorBackend(IGGPOSessionCallbacks callbacks,
            ILog logger,
            int localPort,
            int numPlayers,
            int inputSize,
            string hostIp,
            int hostPort)
        {
            this.callbacks = callbacks;
            this.logger = logger;
            this.numPlayers = numPlayers;
            this.inputSize = inputSize;

            // Initialize the UDP port
            var udpEndpoint = new IPEndPoint(IPAddress.Any, localPort);
            udp = new UdpClient(udpEndpoint);
            udp.BeginReceive(new System.AsyncCallback(OnMessage), null);

            // Initialize the host endpoint
            host = new UdpProtocol(udp, poll, 0, hostIp, hostPort, null, logger);
        }

        public void OnMessage(IAsyncResult res)
        {
            var endpoint = (IPEndPoint)res.AsyncState;

            byte[] receiveBytes = udp.EndReceive(res, ref endpoint);
            udp.BeginReceive(new System.AsyncCallback(OnMessage), endpoint);

            var msg = Utility.Deserialize<NetworkMessage>(receiveBytes);

            if (host.HandlesMessage(endpoint, msg))
            {
                host.OnMessage(msg);
            }
        }

        public GGPOErrorCode DoPoll(int timeout)
        {
            poll.Pump(0);
            PollUdpProtocolEvents();
            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode AddLocalInput(int playerHandle, byte[] values)
        {
            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode AddPlayer(GGPOPlayer player, out int playerHandle)
        {
            playerHandle = 0;
            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode SyncInput(ref byte[] values, ref int disconnectFlags)
        {
            disconnectFlags = 0;

            // Wait until we've started to return inputs.
            if (!isSynchronizing)
            {
                return GGPOErrorCode.NotSynchronized;
            }

            GameInput input = inputs[nextInputToSend % SpectatorFrameBufferSize];
            if (input.frame < nextInputToSend)
            {
                // Haven't received the input from the host yet.  Wait
                return GGPOErrorCode.PredictionThreshold;
            }

            if (input.frame > nextInputToSend)
            {
                // The host is way way way far ahead of the spectator.  How'd this
                // happen?  Anyway, the input we need is gone forever.
                return GGPOErrorCode.GeneralFailure;
            }

            Debug.Assert(values.Length >= inputSize * numPlayers);
            Unsafe.CopyBlock(ref values[0], ref input.bits[0], (uint)(inputSize * numPlayers));
            nextInputToSend++;

            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode AdvanceFrame()
        {
            Log($"End of frame ({nextInputToSend - 1})...");
            DoPoll(0);
            PollUdpProtocolEvents();

            return GGPOErrorCode.OK;
        }

        protected void PollUdpProtocolEvents()
        {
            while (host.GetEvent(out UdpProtocolEvent evt))
            {
                OnUdpProtocolEvent(evt);
            }
        }

        protected void OnUdpProtocolEvent(UdpProtocolEvent evt)
        {
            switch (evt.EventType)
            {
                case UdpProtocolEvent.Type.Connected:
                    callbacks.OnConnected(0);
                    break;

                case UdpProtocolEvent.Type.Synchronizing:
                    var syncEvt = evt as SynchronizingEvent;
                    callbacks.OnSynchronizing(0, syncEvt.Count, syncEvt.Total);
                    break;

                case UdpProtocolEvent.Type.Synchronized:
                    if (isSynchronizing)
                    {
                        callbacks.OnSyncrhonized(0);
                        callbacks.OnRunning();
                        isSynchronizing = false;
                    }
                    break;

                case UdpProtocolEvent.Type.NetworkInterrupted:
                    var netEvt = evt as NetworkInterruptedEvent;
                    callbacks.OnConnectionInterrupted(0, netEvt.DisconnectTimeout);
                    break;

                case UdpProtocolEvent.Type.NetworkResumed:
                    callbacks.OnConnectionResumed(0);
                    break;

                case UdpProtocolEvent.Type.Disconnected:
                    callbacks.OnDisconnected(0);
                    break;

                case UdpProtocolEvent.Type.Input:
                    var inputEvt = evt as InputEvent;

                    host.SetLocalFrameNumber(inputEvt.Input.frame);
                    host.SendInputAck();
                    inputs[inputEvt.Input.frame % SpectatorFrameBufferSize] = inputEvt.Input;
                    break;
            }
        }
    }
}
