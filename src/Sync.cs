using GGPOSharp.Interfaces;
using GGPOSharp.Logger;
using GGPOSharp.Network;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GGPOSharp
{
    public class Sync
    {
        // TODO: Allow other people to change this logger
        private static readonly ILog Logger = ConsoleLogger.GetLogger();

        public struct SavedFrame
        {
            public byte[] buffer;
            public int frame;
            public int checksum;

            public static SavedFrame Create()
            {
                return new SavedFrame { frame = GameInput.NullFrame };
            }
        }

        protected struct SavedState
        {
            public SavedFrame[] frames;
            public int head;

            public SavedState(int maxPredictionFrames)
            {
                head = 0;
                frames = new SavedFrame[maxPredictionFrames + 2];
                for (int i = 0; i < frames.Length; i++)
                {
                    frames[i] = SavedFrame.Create();
                }
            }
        }

        public struct Config
        {
            public IGGPOSessionCallbacks callbacks;
            public int numPredictionFrames;
            public int numPlayers;
            public int inputSize;
        }

        public int FrameCount { get; protected set; }

        public bool InRollback { get; protected set; }

        protected int lastConfirmedFrame;
        protected int maxPredictionFrames;

        protected IGGPOSessionCallbacks callbacks;
        protected SavedState savedState;
        protected Config config;
        protected InputQueue[] inputQueues;
        protected NetworkConnectStatus[] localConnectStatus;

        public Sync(Config config)
        {
            // TODO
            this.config = config;
            callbacks = config.callbacks;
            FrameCount = 0;
            InRollback = false;

            maxPredictionFrames = config.numPredictionFrames;

            CreateQueues(config);
        }

        public void SetLastConfirmedFrame(int frame)
        {
            lastConfirmedFrame = frame;
            if (lastConfirmedFrame > 0)
            {
                for (int i = 0; i < config.numPlayers; i++)
                {
                    inputQueues[i].DiscardConfirmedFrames(frame - 1);
                }
            }
        }

        public void SetFrameDelay(int queue, int delay)
        {
            inputQueues[queue].FrameDelay = delay;
        }

        public bool AddLocalInput(int queue, ref GameInput input)
        {
            int framesBehind = FrameCount - lastConfirmedFrame;
            if (FrameCount >= maxPredictionFrames && framesBehind >= maxPredictionFrames)
            {
                Log("Rejecting input from emulator: reached prediction barrier.");
                return false;
            }

            if (FrameCount == 0)
            {
                SaveCurrentFrame();
            }

            Log($"Sending undelayed local frame {FrameCount} to queue {queue}.");
            input.frame = FrameCount;
            inputQueues[queue].AddInput(ref input);

            return true;
        }

        public void AddRemoteInput(int queue, ref GameInput input)
        {
            inputQueues[queue].AddInput(ref input);
        }

        public int GetConfirmedInputs(byte[] values, int frame)
        {
            int disconnectFlags = 0;

            Debug.Assert(values.Length >= config.numPlayers * config.inputSize);

            Unsafe.InitBlock(ref values[0], 0, (uint)values.Length);
            for (int i = 0; i < config.numPlayers; i++)
            {
                var input = new GameInput(GameInput.NullFrame, null, (uint)config.inputSize);
                if (localConnectStatus[i].Disconnected && frame > localConnectStatus[i].LastFrame)
                {
                    disconnectFlags |= (1 << i);
                }
                else
                {
                    inputQueues[i].GetConfirmedInput(frame, ref input);
                }
                Unsafe.CopyBlock(ref values[i * config.inputSize], ref input.bits[0], (uint)config.inputSize);
            }

            return disconnectFlags;
        }

        public int SyncrhonizeInputs(byte[] values)
        {
            int disconnect_flags = 0;

            Debug.Assert(values.Length >= config.numPlayers * config.inputSize);

            Unsafe.InitBlock(ref values[0], 0, (uint)values.Length);
            for (int i = 0; i < config.numPlayers; i++)
            {
                var input = new GameInput(GameInput.NullFrame, null, (uint)config.inputSize);
                if (localConnectStatus[i].Disconnected && FrameCount > localConnectStatus[i].LastFrame)
                {
                    disconnect_flags |= (1 << i);
                }
                else
                {
                    inputQueues[i].GetInput(FrameCount, ref input);
                }
                Unsafe.CopyBlock(ref values[i * config.inputSize], ref input.bits[0], (uint)config.inputSize);
            }
            return disconnect_flags;
        }

        public void CheckSimulation(int timeout)
        {
            if (!CheckSimulationConsistency(out int seekTo))
            {
                AdjustSimulation(seekTo);
            }
        }

        public void AdjustSimulation(int seekTo)
        {
            int frameCount = FrameCount;
            int count = FrameCount - seekTo;

            Log("Catching up");
            InRollback = true;

            // Flush our input queue and load the last frame.
            LoadFrame(seekTo);
            Debug.Assert(FrameCount == seekTo);

            // Advance frame by frame (stuffing notifications back to 
            // the master).
            ResetPrediction(FrameCount);
            for (int i = 0; i < count; i++)
            {
                callbacks.AdvanceFrame();
            }
            Debug.Assert(FrameCount == frameCount);

            InRollback = false;

            Log("---");
        }

        public void IncrementFrame()
        {
            FrameCount++;
            SaveCurrentFrame();
        }

        public void LoadFrame(int frame)
        {
            // Find the frame in question
            if (frame == FrameCount)
            {
                Log("Skipping NOP.");
                return;
            }

            // Move the head pointer back and load it up
            savedState.head = FindSavedFrameIndex(frame);
            ref SavedFrame state = ref savedState.frames[savedState.head];

            Log($"=== Loading frame info {state.frame} (size: {state.buffer.Length}  checksum: {state.checksum:X8}).");

            Debug.Assert(state.buffer != null);
            callbacks.LoadGameState(state.buffer);

            // Reset framecount and the head of the state ring-buffer to point in
            // advance of the current frame (as if we had just finished executing it).
            FrameCount = state.frame;
            savedState.head = (savedState.head + 1) % savedState.frames.Length;
        }

        public void SaveCurrentFrame()
        {
            // See StateCompress for the real save feature implemented by FinalBurn.
            // Write everything into the head, then advance the head pointer.
            ref SavedFrame state = ref savedState.frames[savedState.head];
            state.frame = FrameCount;
            callbacks.SaveGameState(ref state);

            Log($"=== Saved frame info {state.frame} (size: {state.buffer.Length}  checksum: {state.checksum:X8}).");
            savedState.head = (savedState.head + 1) % savedState.frames.Length;
        }

        protected int FindSavedFrameIndex(int frame)
        {
            int i, count = savedState.frames.Length;
            for (i = 0; i < count; i++)
            {
                if (savedState.frames[i].frame == frame)
                {
                    break;
                }
            }

            Debug.Assert(i != count);
            return i;
        }

        public ref SavedFrame GetLastSavedFrame()
        {
            int i = savedState.head - 1;
            if (i < 0)
            {
                i = savedState.frames.Length - 1;
            }
            return ref savedState.frames[i];
        }

        protected bool CreateQueues(in Config config)
        {
            inputQueues = new InputQueue[config.numPlayers];
            for (int i = 0; i < inputQueues.Length; i++)
            {
                inputQueues[i] = new InputQueue(config.inputSize, i);
            }

            return true;
        }

        protected bool CheckSimulationConsistency(out int seekTo)
        {
            int first_incorrect = GameInput.NullFrame;
            for (int i = 0; i < config.numPlayers; i++)
            {
                int incorrect = inputQueues[i].GetFirstIncorrectFrame();
                Log($"considering incorrect frame {incorrect} reported by queue {i}.");

                if (incorrect != GameInput.NullFrame && (first_incorrect == GameInput.NullFrame || incorrect < first_incorrect))
                {
                    first_incorrect = incorrect;
                }
            }

            if (first_incorrect == GameInput.NullFrame)
            {
                Log("prediction ok.  proceeding.");
                seekTo = 0;
                return true;
            }

            seekTo = first_incorrect;
            return false;
        }

        protected void ResetPrediction(int frameNumber)
        {
            for (int i = 0; i < config.numPlayers; i++)
            {
                inputQueues[i].ResetPrediction(frameNumber);
            }
        }

        private void Log(string msg)
        {
            Logger.Log($"{msg}\n");
        }
    }
}
