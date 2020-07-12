using GGPOSharp.Interfaces;
using GGPOSharp.Logger;
using System;
using System.Linq;

namespace GGPOSharp
{
    class TimeSync
    {
        // TODO: Allow other people to change this logger
        private static readonly ILog Logger = ConsoleLogger.GetLogger();

        private static int count = 0;

        public const int FrameWindowSize = 40;
        public const int MinUniqueFrames = 10;
        public const int MinFrameAdvantage = 3;
        public const int MaxFrameAdvantage = 9;

        protected int[] local;
        protected int[] remote;
        protected GameInput[] lastInputs;
        protected int nextPrediction;

        public TimeSync()
        {
            local = new int[FrameWindowSize];
            remote = new int[FrameWindowSize];
            lastInputs = new GameInput[MinUniqueFrames];
            nextPrediction = FrameWindowSize * 3;
        }

        public void AdvanceFrame(ref GameInput input, int advantage, int radvantage)
        {
            // Remember the last frame and frame advantage
            lastInputs[input.frame % lastInputs.Length] = input;
            local[input.frame % local.Length] = advantage;
            remote[input.frame % remote.Length] = radvantage;
        }

        public int RecommendFrameWaitDuration(bool requireIdleInput)
        {
            // Average our local and remote frame advantages
            double advantage = local.Average();
            double radvantage = remote.Average();

            count++;

            // See if someone should take action.  The person furthest ahead
            // needs to slow down so the other user can catch up.
            // Only do this if both clients agree on who's ahead!!
            if (advantage >= radvantage)
            {
                return 0;
            }

            // Both clients agree that we're the one ahead.  Split
            // the difference between the two to figure out how long to
            // sleep for.
            int sleepFrames = (int)(((radvantage - advantage) / 2) + 0.5);

            Logger.Log($"iteration {count}:  sleep frames is {sleepFrames}");

            // Some things just aren't worth correcting for.  Make sure
            // the difference is relevant before proceeding.
            if (sleepFrames < MinFrameAdvantage)
            {
                return 0;
            }

            // Make sure our input had been "idle enough" before recommending
            // a sleep.  This tries to make the emulator sleep while the
            // user's input isn't sweeping in arcs (e.g. fireball motions in
            // Street Fighter), which could cause the player to miss moves.
            if (requireIdleInput)
            {
                for (int i = 1; i < lastInputs.Length; i++)
                {
                    if (!lastInputs[i].Equal(lastInputs[0], true))
                    {
                        Logger.Log($"iteration {count}:  rejecting due to input stuff at position {i}...!!!");
                        return 0;
                    }
                }
            }

            // Success!!! Recommend the number of frames to sleep and adjust
            return Math.Min(sleepFrames, MaxFrameAdvantage);
        }
    }
}
