using GGPOSharp.Interfaces;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GGPOSharp.Backends
{
    public class SyncTestSession : GGPOSession
    {
        private const string LogFileDirectory = "synclogs";
        
        protected struct SavedInfo
        {
            public int frame;
            public int checksum;
            public IGameState buffer;
            public GameInput input;
        };

        protected GameInput currentInput;
        protected GameInput lastInput;
        protected Sync sync;
        protected int lastVerified;
        protected int checkDistance;
        protected bool isRunning = false;
        protected bool isRollingBack = false;

        private RingBuffer<SavedInfo> savedFrames = new RingBuffer<SavedInfo>(32);

        public SyncTestSession(IGGPOSessionCallbacks callbacks, ILog logger, int numPlayers)
        {
            this.callbacks = callbacks;
            this.logger = logger;
            this.numPlayers = numPlayers;
            
            // Initialize the synchronization layer
            sync = new Sync(new Sync.Config
            {
                callbacks = callbacks,
                numPredictionFrames = Constants.MaxPredictionFrames,
            });
        }

        public override GGPOErrorCode Idle(int timeout)
        {
            if (!isRunning)
            {
                callbacks.OnRunning();
                isRunning = true;
            }

            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode AddLocalInput(int playerHandle, byte[] values)
        {
            if (!isRunning)
            {
                return GGPOErrorCode.NotSynchronized;
            }

            for (int i = 0; i < values.Length; i++)
            {
                currentInput.bits[(playerHandle * values.Length) + i] |= values[i];
            }

            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode AddPlayer(GGPOPlayer player, out int playerHandle)
        {
            playerHandle = Constants.InvalidHandle;

            if (player.playerId < 1 || player.playerId > numPlayers)
            {
                return GGPOErrorCode.PlayerOutOfRange;
            }

            playerHandle = player.playerId - 1;
            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode SyncInput(ref byte[] values, ref int disconnectFlags)
        {
            if (isRollingBack)
            {
                lastInput = savedFrames.Front().input;
            }
            else
            {
                if (sync.FrameCount == 0)
                {
                    sync.SaveCurrentFrame();
                }
                lastInput = currentInput;
            }
            Unsafe.CopyBlock(ref values[0], ref lastInput.bits[0], (uint)values.Length);

            disconnectFlags = 0;
            return GGPOErrorCode.OK;
        }

        public override GGPOErrorCode AdvanceFrame()
        {
            sync.IncrementFrame();
            currentInput.Erase();

            Log($"End of frame({sync.FrameCount})...");

            if (isRollingBack)
            {
                return GGPOErrorCode.OK;
            }

            int frame = sync.FrameCount;
            // Hold onto the current frame in our queue of saved states.  We'll need
            // the checksum later to verify that our replay of the same frame got the
            // same results.
            var info = new SavedInfo
            {
                frame = frame,
                input = lastInput,
                buffer = sync.GetLastSavedFrame().buffer,
                checksum = sync.GetLastSavedFrame().checksum,
            };
            savedFrames.Push(info);

            if (frame - lastVerified == checkDistance)
            {
                // We've gone far enough ahead and should now start replaying frames.
                // Load the last verified frame and set the rollback flag to true.
                sync.LoadFrame(lastVerified);

                isRollingBack = true;
                while (!savedFrames.IsEmpty)
                {
                    callbacks.AdvanceFrame();

                    // Verify that the checksumn of this frame is the same as the one in our
                    // list.
                    info = savedFrames.Front();
                    savedFrames.Pop();

                    if (info.frame != sync.FrameCount)
                    {
                        RaiseSyncError($"Frame number {info.frame} does not match saved frame number {frame}");
                    }
                    int checksum = sync.GetLastSavedFrame().checksum;
                    if (info.checksum != checksum)
                    {
                        LogSaveStates(info);
                        RaiseSyncError($"Checksum for frame {frame} does not match saved ({checksum} != {info.checksum})");
                    }
                    Log($"Checksum {checksum:D8} for frame {info.frame} matches.");
                }
                lastVerified = frame;
                isRollingBack = false;
            }

            return GGPOErrorCode.OK;
        }

        protected void RaiseSyncError(string msg)
        {
            Log(msg);
            Debug.Fail(msg);
        }

        protected void LogSaveStates(in SavedInfo info)
        {
            callbacks.LogGameState($"{LogFileDirectory}\\state-{sync.FrameCount:D4}-original.log", info.buffer);
            callbacks.LogGameState($"{LogFileDirectory}\\state-{sync.FrameCount:D4}-replay.log", sync.GetLastSavedFrame().buffer);
        }
    }
}
