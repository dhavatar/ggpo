using GGPOSharp.Interfaces;
using GGPOSharp.Network.Events;
using GGPOSharp.Network.Messages;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace GGPOSharp.Network
{
    public class UdpProtocol
    {
        public struct Stats
        {
            public int ping;
            public int remoteFrameAdvantage;
            public int localFrameAdvantage;
            public int sendQueueLen;
            public int bytesSent;
            public int packetsSent;
            public float kbpsSent;
        }

        protected struct SyncState
        {
            public int roundTripsRemaining;
            public int random;
        }

        protected struct RunningState
        {
            public long lastQualityReportTime;
            public long lastNetworkStatsInterval;
            public long lastInputPacketReceiveTime;
        }

        protected enum State
        {
            Syncing,
            Synchronized,
            Running,
            Disconnected,
        }

        protected struct QueueEntry
        {
            public long queueTime;
            public IPAddress destAddress;
            public NetworkMessage message;
        }

        public int DisconnectTimeout { get; set; }
        public int DisconnectNotifyStart { get; set; }

        public bool IsInitialized => udpClient != null;
        public bool IsRunning => currentState == State.Running;
        public bool IsSynchronized => currentState == State.Synchronized;

        // Network transmission information
        UdpClient udpClient;
        IPAddress peerAddress;
        ushort magicNumber;
        int queue = -1;
        ushort remoteMagicNumber;
        bool connected = false;
        int sendLatency;
        int oopPercent;
        QueueEntry ooPacket;
        RingBuffer<QueueEntry> sendQueue = new RingBuffer<QueueEntry>(64);

        // Fairness
        int localFrameAdvantage;
        int remoteFrameAdvantage;

        // Stats
        long roundTripTime;
        int packetsSent;
        long bytesSent;
        int kbpsSent;
        long statsStartTime;

        // The state machine
        NetworkConnectStatus[] localConnectStatus;
        NetworkConnectStatus[] peerConnectStatus;
        State currentState;
        SyncState syncState;
        RunningState runningState;

        // Packet loss
        RingBuffer<GameInput> pendingOutput = new RingBuffer<GameInput>(64);
        GameInput lastReceivedInput = new GameInput(-1, null, 1);
        GameInput lastSentInput = new GameInput(-1, null, 1);
        GameInput lastAckedInput = new GameInput(-1, null, 1);
        long lastSendTime;
        long lastReceiveTime;
        long shutdownTimeout;
        bool disconnectNotifySent = false;
        bool disconnectEventSent = false;

        ushort nextSendSequence;
        ushort nextReceiveSequence;

        // Rift synchronization
        TimeSync timeSync;

        // Event queue
        RingBuffer<UdpProtocolEvent> eventQueue = new RingBuffer<UdpProtocolEvent>(64);

        ILog logger;
        Random random = new Random();

        public UdpProtocol(int queue, string ipString, int port, NetworkConnectStatus[] status, ILog logger)
        {
            peerConnectStatus = new NetworkConnectStatus[Constants.MaxPlayers];
            localConnectStatus = status;
            this.logger = logger;

            udpClient = new UdpClient();
            IPAddress ipAddress = IPAddress.Parse(ipString);
            try
            {
                udpClient.Connect(ipAddress, port);
            }
            catch (Exception e)
            {
                // TODO: Better logging
                Console.WriteLine(e.ToString());
            }

            do
            {
                magicNumber = (ushort)random.Next();
            } while (magicNumber == 0);
        }

        public void Disconnect()
        {
            currentState = State.Disconnected;
            shutdownTimeout = Utility.GetCurrentTime() + NetworkConstants.UpdShutdownTimer;
        }

        public bool GetEvent(out UdpProtocolEvent evt)
        {
            if (eventQueue.IsEmpty)
            {
                evt = null;
                return false;
            }

            evt = eventQueue.Front();
            eventQueue.Pop();
            return true;
        }

        public void Synchronize()
        {
            currentState = State.Syncing;
            syncState.roundTripsRemaining = NetworkConstants.NumSyncPackets;
            SendSyncRequest();
        }

        public bool GetPeerConnectStatus(int id, out int frame)
        {
            frame = peerConnectStatus[id].LastFrame;
            return !peerConnectStatus[id].Disconnected;
        }

        public void OnMessage(NetworkMessage msg)
        {
            // TODO
        }

        public void Update()
        {
            long now = Utility.GetCurrentTime();
            int nextInterval;

            PumpSendQueue();
            switch (currentState)
            {
                case State.Syncing:
                    nextInterval = (syncState.roundTripsRemaining == NetworkConstants.NumSyncPackets)
                        ? NetworkConstants.SyncFirstRetryInterval
                        : NetworkConstants.SyncRetryInterval;
                    if (lastSendTime > 0 && lastSendTime + nextInterval < now)
                    {
                        logger.Log($"No luck syncing after {nextInterval} ms... Re - queueing sync packet.");
                        SendSyncRequest();
                    }
                    break;

                case State.Running:
                    if (runningState.lastInputPacketReceiveTime > 0 || runningState.lastInputPacketReceiveTime + NetworkConstants.RunningRetryInterval < now)
                    {
                        logger.Log($"Haven't exchanged packets in a while (last received:{lastReceivedInput.frame}  last sent:{lastSentInput.frame}).  Resending.");
                        SendPendingOutput();
                        runningState.lastInputPacketReceiveTime = now;
                    }

                    if (runningState.lastQualityReportTime > 0 || runningState.lastQualityReportTime + NetworkConstants.QualityReportInterval < now)
                    {
                        var msg = new QualityReportMessage
                        {
                            Ping = Utility.GetCurrentTime(),
                            FrameAdvantage = (byte)localFrameAdvantage,
                        };
                        SendMessage(msg);
                        runningState.lastQualityReportTime = now;
                    }

                    if (runningState.lastNetworkStatsInterval > 0 || runningState.lastNetworkStatsInterval + NetworkConstants.NetworkStatsInterval < now)
                    {
                        UpdateNetworkStats();
                        runningState.lastNetworkStatsInterval = now;
                    }

                    if (lastSendTime > 0 && lastSendTime + NetworkConstants.KeepAliveInterval < now)
                    {
                        logger.Log("Sending keep alive packet");
                        SendMessage(new KeepAliveMessage());
                    }

                    if (DisconnectTimeout > 0 &&
                        DisconnectNotifyStart > 0 &&
                        !disconnectNotifySent &&
                        lastReceiveTime + DisconnectNotifyStart < now)
                    {
                        logger.Log($"Endpoint has stopped receiving packets for {DisconnectNotifyStart} ms.  Sending notification.");

                        var e = new NetworkInterruptedEvent
                        {
                            DisconnectTimeout = DisconnectTimeout - DisconnectNotifyStart
                        };
                        QueueEvent(e);
                        disconnectNotifySent = true;
                    }

                    if (DisconnectTimeout > 0 && 
                        lastReceiveTime + DisconnectTimeout < now &&
                        !disconnectEventSent)
                    {
                        logger.Log($"Endpoint has stopped receiving packets for {DisconnectTimeout} ms.  Disconnecting.");

                        QueueEvent(new UdpProtocolEvent(UdpProtocolEvent.Type.Disconnected));
                        disconnectEventSent = true;
                    }
                    break;

                case State.Disconnected:
                    if (shutdownTimeout < now)
                    {
                        logger.Log("Shutting down udp connection.");
                        udpClient.Close();
                        shutdownTimeout = 0;
                    }
                    break;
            }
        }

        public void SetLocalFrameNumber(int localFrame)
        {
            // Estimate which frame the other guy is one by looking at the
            // last frame they gave us plus some delta for the one-way packet
            // trip time.
            long remoteFrame = lastReceivedInput.frame + (roundTripTime * 60 / 1000);

            // Our frame advantage is how many frames *behind* the other guy
            // we are.  Counter-intuative, I know.  It's an advantage because
            // it means they'll have to predict more often and our moves will
            // pop more frequenetly.
            localFrameAdvantage = (int)remoteFrame - localFrame;
        }

        public int RecommendFrameDelay()
        {
            // XXX: require idle input should be a configuration parameter
            return timeSync.RecommendFrameWaitDuration(false);
        }

        public void SendInput(ref GameInput input)
        {
            if (currentState == State.Running)
            {
                // Check to see if this is a good time to adjust for the rift...
                timeSync.AdvanceFrame(ref input, localFrameAdvantage, remoteFrameAdvantage);

                // Save this input packet
                // 
                // XXX: This queue may fill up for spectators who do not ack input packets in a timely
                // manner.When this happens, we can either resize the queue(ug) or disconnect them
                // (better, but still ug).  For the meantime, make this queue really big to decrease
                // the odds of this happening...
                pendingOutput.Push(input);
            }

            SendPendingOutput();
        }

        public void SendInputAck()
        {
            var msg = new InputAckMessage
            {
                AckFrame = lastReceivedInput.frame,
            };

            SendMessage(msg);
        }

        public void GetNetworkStats(out GGPONetworkStats stats)
        {
            stats = new GGPONetworkStats
            {
                Ping = roundTripTime,
                SendQueueLenth = pendingOutput.Size,
                KbpsSent = kbpsSent,
                RemoteFramesBehind = remoteFrameAdvantage,
                LocalFramesBehind = localFrameAdvantage,
            };
        }

        protected void SendPendingOutput()
        {
            var msg = new InputMessage();
            int offset = 0;
            GameInput last = lastAckedInput;

            if (!pendingOutput.IsEmpty)
            {   
                msg.StartFrame = pendingOutput.Front().frame;
                msg.InputSize = pendingOutput.Front().size;

                Debug.Assert(last.frame == -1 || last.frame + 1 == msg.StartFrame);

                for (int j = 0; j < pendingOutput.Size; j++)
                {
                    GameInput current = pendingOutput[j];
                    if (!current.Equal(last, true))
                    {
                        Debug.Assert((GameInput.MaxBytes * GameInput.MaxPlayers * 8) < (1 << Bitvector.NibbleSize));

                        for (int i = 0; i < current.size * 8; i++)
                        {
                            Debug.Assert(i < (i << Bitvector.NibbleSize));
                            if (current[i] != last[i])
                            {
                                Bitvector.SetBit(msg.Bits, ref offset);
                                if (current[i])
                                {
                                    Bitvector.SetBit(msg.Bits, ref offset);
                                }
                                else
                                {
                                    Bitvector.ClearBit(msg.Bits, ref offset);
                                }
                                Bitvector.WriteNibblet(msg.Bits, i, ref offset);
                            }
                        }
                    }

                    Bitvector.ClearBit(msg.Bits, ref offset);
                    last = lastSentInput = current;
                }
            }
            else
            {
                msg.StartFrame = 0;
                msg.InputSize = 0;
            }

            msg.AckFrame = lastReceivedInput.frame;
            msg.NumBits = (ushort)offset;

            msg.DisconnectRequested = currentState == State.Disconnected;
            if (localConnectStatus != null)
            {
                for (int i = 0; i < Constants.MaxPlayers; i++)
                {
                    msg.PeerConnectStatus[i].Copy(localConnectStatus[i]);
                }
            }
            else
            {
                for (int i = 0; i < Constants.MaxPlayers; i++)
                {
                    msg.PeerConnectStatus[i].Reset();
                }
            }

            Debug.Assert(offset < Constants.MaxCompressedBits);

            SendMessage(msg);
        }

        protected void SendSyncRequest()
        {
            syncState.random = random.Next() & 0xFFFF;
            var msg = new SyncRequestMessage
            {
                RandomRequest = syncState.random,
            };
            SendMessage(msg);
        }

        protected void SendMessage(NetworkMessage msg)
        {
            LogMsg("send", msg);

            packetsSent++;
            lastSendTime = Utility.GetCurrentTime();
            bytesSent += Utility.GetMessageSize(msg);

            msg.Magic = magicNumber;
            msg.SequenceNumber = nextSendSequence++;

            sendQueue.Push(new QueueEntry
            {
                queueTime = Utility.GetCurrentTime(),
                destAddress = peerAddress,
                message = msg,
            });

            PumpSendQueue();
        }

        protected void UpdateNetworkStats()
        {
            long now = Utility.GetCurrentTime();

            if (statsStartTime == 0)
            {
                statsStartTime = now;
            }

            float seconds = (now - statsStartTime) / 1000f;

        }

        protected void QueueEvent(UdpProtocolEvent evt)
        {
            LogEvent("Queuing event", evt);
            eventQueue.Push(evt);
        }

        protected void PumpSendQueue()
        {
            while (!sendQueue.IsEmpty)
            {
                QueueEntry entry = sendQueue.Front();

                if (sendLatency > 0)
                {
                    // Should really come up with a gaussian distributation based on the configured
                    // value, but this will do for now.
                    int jitter = (sendLatency * 2 / 3) + ((random.Next() % sendLatency) / 3);
                    if (Utility.GetCurrentTime() < sendQueue.Front().queueTime + jitter)
                    {
                        break;
                    }
                }

                if (oopPercent > 0 && ooPacket.message != null && random.Next() % 100 < oopPercent)
                {
                    int delay = random.Next() % (sendLatency * 10 + 1000);
                    logger.Log($"creating rogue oop (seq: {entry.message.SequenceNumber}  delay: {delay})");
                    ooPacket.queueTime = Utility.GetCurrentTime() + delay;
                    ooPacket.message = entry.message;
                    ooPacket.destAddress = entry.destAddress;
                }
                else
                {
                    var byteMsg = Utility.GetByteArray(entry.message);
                    udpClient.Send(byteMsg, byteMsg.Length);

                    entry.message = null;
                }

                sendQueue.Pop();
            }

            if (ooPacket.message != null && ooPacket.queueTime < Utility.GetCurrentTime())
            {
                logger.Log("sending rogue oop!");
                var ooMsg = Utility.GetByteArray(ooPacket.message);
                udpClient.Send(ooMsg, ooMsg.Length);

                ooPacket.message = null;
            }
        }

        protected void ClearSendQueue()
        {
            while (!sendQueue.IsEmpty)
            {
                sendQueue.Front().message = null;
                sendQueue.Pop();
            }
        }

        protected void LogMsg(string prefix, NetworkMessage msg)
        {
            logger.Log($"{prefix} {msg.GetLogMessage()}");
        }

        protected void LogEvent(string prefix, UdpProtocolEvent evt)
        {
            if (evt.EventType == UdpProtocolEvent.Type.Synchronized)
            {
                logger.Log($"{prefix} (event: Syncrhonized).");
            }
        }

        protected bool OnInvalid(NetworkMessage msg)
        {
            Debug.Assert(false, "Invalid msg in UdpProtocol");
            return false;
        }

        protected bool OnSyncRequest(NetworkMessage msg)
        {
            if (remoteMagicNumber != 0 && msg.Magic != remoteMagicNumber)
            {
                logger.Log($"Ignoring sync request from unknown endpoint ({msg.Magic} != {remoteMagicNumber}).");
                return false;
            }

            var reply = new SyncReplyMessage
            {
                RandomReply = (msg as SyncRequestMessage).RandomRequest,
            };
            SendMessage(reply);
            return true;
        }

        protected bool OnSyncReply(NetworkMessage msg)
        {
            if (currentState != State.Syncing)
            {
                logger.Log("Ignoring SyncReply while not synching.");
                return msg.Magic == remoteMagicNumber;
            }

            var syncMsg = msg as SyncReplyMessage;
            if (syncMsg.RandomReply != syncState.random)
            {
                logger.Log($"sync reply {syncMsg.RandomReply} != {syncState.random}.  Keep looking...");
                return false;
            }

            if (!connected)
            {
                QueueEvent(new UdpProtocolEvent(UdpProtocolEvent.Type.Connected));
                connected = true;
            }

            logger.Log($"Checking sync state ({syncState.roundTripsRemaining} round trips remaining).");
            if (--syncState.roundTripsRemaining == 0)
            {
                logger.Log("Syncrhonized!");
                QueueEvent(new UdpProtocolEvent(UdpProtocolEvent.Type.Synchronized));
                currentState = State.Running;
                lastReceivedInput.frame = GameInput.NullFrame;
                remoteMagicNumber = syncMsg.Magic;
            }
            else
            {
                QueueEvent(new SynchronizingEvent
                {
                    Total = NetworkConstants.NumSyncPackets,
                    Count = NetworkConstants.NumSyncPackets - syncState.roundTripsRemaining,
                });
                SendSyncRequest();
            }

            return true;
        }

        protected bool OnInput(NetworkMessage msg)
        {
            var inputMsg = msg as InputMessage;

            // If a disconnect is requested, go ahead and disconnect now.
            bool disconnectRequested = inputMsg.DisconnectRequested;
            if (disconnectRequested)
            {
                if (currentState != State.Disconnected && !disconnectEventSent)
                {
                    logger.Log("Disconnecting endpoint on remote request.");
                    QueueEvent(new UdpProtocolEvent(UdpProtocolEvent.Type.Disconnected));
                    disconnectEventSent = true;
                }
            }
            else
            {
                // Update the peer connection status if this peer is still considered to be part
                // of the network.
                NetworkConnectStatus[] remoteStatus = inputMsg.PeerConnectStatus;
                for (int i = 0; i < peerConnectStatus.Length; i++)
                {
                    Debug.Assert(remoteStatus[i].LastFrame >= peerConnectStatus[i].LastFrame);
                    peerConnectStatus[i].Disconnected = peerConnectStatus[i].Disconnected || remoteStatus[i].Disconnected;
                    peerConnectStatus[i].LastFrame = Math.Max(peerConnectStatus[i].LastFrame, remoteStatus[i].LastFrame);
                }
            }

            // Decompress the input.
            int lastReceivedFrameNumber = lastReceivedInput.frame;
            if (inputMsg.NumBits > 0)
            {
                int offset = 0;
                int currentFrame = inputMsg.StartFrame;

                lastReceivedInput.size = inputMsg.InputSize;
                if (lastReceivedInput.frame < 0)
                {
                    lastReceivedInput.frame = inputMsg.StartFrame - 1;
                }

                while (offset < inputMsg.NumBits)
                {
                    // Keep walking through the frames (parsing bits) until we reach
                    // the inputs for the frame right after the one we're on.
                    Debug.Assert(currentFrame <= lastReceivedInput.frame + 1);
                    bool useInputs = currentFrame == lastReceivedInput.frame + 1;

                    while (Bitvector.ReadBit(inputMsg.Bits, ref offset) > 0)
                    {
                        bool on = Bitvector.ReadBit(inputMsg.Bits, ref offset) > 0;
                        int button = Bitvector.ReadBit(inputMsg.Bits, ref offset);

                        if (useInputs)
                        {
                            if (on)
                            {
                                lastReceivedInput.Set(button);
                            }
                            else
                            {
                                lastReceivedInput.Clear(button);
                            }
                        }
                    }
                    Debug.Assert(offset <= inputMsg.NumBits);

                    // Now if we want to use these inputs, go ahead and send them to
                    // the emulator.
                    if (useInputs)
                    {
                        // Move forward 1 frame in the stream.
                        Debug.Assert(currentFrame == lastReceivedInput.frame + 1);
                        lastReceivedInput.frame = currentFrame;

                        // Send the event to the emulator
                        var evt = new InputEvent
                        {
                            Input = lastReceivedInput,
                        };

                        runningState.lastInputPacketReceiveTime = Utility.GetCurrentTime();
                        logger.Log($"Sending frame {lastReceivedInput.frame} to emu queue {queue} ({lastReceivedInput.ToString()}).");
                        QueueEvent(evt);
                    }
                    else
                    {
                        logger.Log($"Skipping past frame:({currentFrame}) current is {lastReceivedInput.frame}.");
                    }

                    // Move forward 1 frame in the input stream.
                    currentFrame++;
                }
            }

            Debug.Assert(lastReceivedInput.frame >= lastReceivedFrameNumber);

            // Get rid of our buffered input
            while (!pendingOutput.IsEmpty && pendingOutput.Front().frame < inputMsg.AckFrame)
            {
                logger.Log($"Throwing away pending output frame {pendingOutput.Front().frame}");
                lastAckedInput = pendingOutput.Front();
                pendingOutput.Pop();
            }

            return true;
        }

        protected bool OnInputAck(NetworkMessage msg)
        {
            var inputMsg = msg as InputAckMessage;

            // Get rid of our buffered input
            while (!pendingOutput.IsEmpty && pendingOutput.Front().frame < inputMsg.AckFrame)
            {
                logger.Log($"Throwing away pending output frame {pendingOutput.Front().frame}");
                lastAckedInput = pendingOutput.Front();
                pendingOutput.Pop();
            }

            return true;
        }

        protected bool OnQualityReport(NetworkMessage msg)
        {
            var qualityMsg = msg as QualityReportMessage;

            // Send a reply so the other side can compute the round trip transmit time.
            var reply = new QualityReplyMessage
            {
                Pong = qualityMsg.Ping,
            };
            SendMessage(reply);

            remoteFrameAdvantage = qualityMsg.FrameAdvantage;
            return true;
        }

        protected bool OnQualityReply(NetworkMessage msg)
        {
            roundTripTime = Utility.GetCurrentTime() - (msg as QualityReplyMessage).Pong;
            return true;
        }

        protected bool OnKeepAlive(NetworkMessage msg)
        {
            return true;
        }
    }
}
