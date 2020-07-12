using GGPOSharp.Interfaces;
using GGPOSharp.Logger;
using System;
using System.Diagnostics;

namespace GGPOSharp
{
    public class InputQueue
    {
        // TODO: Allow other people to change this logger
        private static readonly ILog Logger = ConsoleLogger.GetLogger();

        public const int DefaultInputQueueLength = 128;
        public const int DefaultInputSize = 4;

        public int FrameDelay { get; set; }

        protected int id;
        protected int head;
        protected int tail;
        protected int length;
        protected bool firstFrame;
        protected int lastUserAddedFrame;
        protected int lastAddedFrame;
        protected int firstIncorrectFrame;
        protected int lastFrameRequested;

        protected GameInput[] inputs;
        protected GameInput prediction;

        public InputQueue(int inputSize = DefaultInputSize, int id = -1)
        {
            this.id = id;
            head = tail = FrameDelay = 0;
            firstFrame = true;

            lastUserAddedFrame = GameInput.NullFrame;
            lastAddedFrame = GameInput.NullFrame;
            firstIncorrectFrame = GameInput.NullFrame;
            lastFrameRequested = GameInput.NullFrame;

            prediction = new GameInput(GameInput.NullFrame, null, (uint)inputSize);
            inputs = new GameInput[DefaultInputQueueLength];
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = new GameInput(GameInput.NullFrame, null, DefaultInputSize);
            }
        }

        public int GetLastConfirmedFrame()
        {
            Log($"returning last confirmed frame {lastAddedFrame}.");
            return lastAddedFrame;
        }

        public int GetFirstIncorrectFrame()
        {
            return firstIncorrectFrame;
        }

        public void DiscardConfirmedFrames(int frame)
        {
            Debug.Assert(frame >= 0);

            if (lastFrameRequested != GameInput.NullFrame)
            {
                frame = Math.Min(frame, lastFrameRequested);
            }

            Log($"discarding confirmed frames up to {frame} (last_added:{lastAddedFrame} length:{length} [head:{head} tail:{tail}]).");
            if (frame >= lastAddedFrame)
            {
                tail = head;
            }
            else
            {
                int offset = frame - inputs[tail].frame + 1;

                Log($"difference of {offset} frames.");
                Debug.Assert(offset >= 0);

                tail = (tail + offset) % inputs.Length;
                length -= offset;
            }

            Log($"after discarding, new tail is {tail} (frame:{inputs[tail].frame}).");
            Debug.Assert(length >= 0);
        }

        public void ResetPrediction(int frame)
        {
            Debug.Assert(firstIncorrectFrame == GameInput.NullFrame || frame <= firstIncorrectFrame);

            Log($"resetting all prediction errors back to frame {frame}.");

            // There's nothing really to do other than reset our prediction
            // state and the incorrect frame counter...
            prediction.frame = GameInput.NullFrame;
            firstIncorrectFrame = GameInput.NullFrame;
            lastFrameRequested = GameInput.NullFrame;
        }

        public bool GetConfirmedInput(int requestedFrame, ref GameInput input)
        {
            Debug.Assert(firstIncorrectFrame == GameInput.NullFrame || requestedFrame < firstIncorrectFrame);
            int offset = requestedFrame % inputs.Length;
            if (inputs[offset].frame != requestedFrame)
            {
                return false;
            }
            input = inputs[offset];
            return true;
        }

        public bool GetInput(int requestedFrame, ref GameInput input)
        {
            Log($"requesting input frame {requestedFrame}.");

            
            // No one should ever try to grab any input when we have a prediction
            // error.  Doing so means that we're just going further down the wrong
            // path.  ASSERT this to verify that it's true.
            Debug.Assert(firstIncorrectFrame == GameInput.NullFrame);

            // Remember the last requested frame number for later.  We'll need
            // this in AddInput() to drop out of prediction mode.
            lastFrameRequested = requestedFrame;

            Debug.Assert(requestedFrame >= inputs[tail].frame);

            if (prediction.frame == GameInput.NullFrame)
            {
                // If the frame requested is in our range, fetch it out of the queue and
                // return it.
                int offset = requestedFrame - inputs[tail].frame;

                if (offset < length)
                {
                    offset = (offset + tail) % inputs.Length;
                    Debug.Assert(inputs[offset].frame == requestedFrame);
                    input = inputs[offset];
                    Log($"returning confirmed frame number {input.frame}.");
                    return true;
                }

                // The requested frame isn't in the queue.  Bummer.  This means we need
                // to return a prediction frame.  Predict that the user will do the
                // same thing they did last time.
                if (requestedFrame == 0)
                {
                    Log("basing new prediction frame from nothing, you're client wants frame 0.");
                    prediction.Erase();
                }
                else if (lastAddedFrame == GameInput.NullFrame)
                {
                    Log("basing new prediction frame from nothing, since we have no frames yet.");
                    prediction.Erase();
                }
                else
                {
                    Log($"basing new prediction frame from previously added frame (queue entry:{PreviousFrame(head)}, frame:{inputs[PreviousFrame(head)].frame}).");
                    prediction = inputs[PreviousFrame(head)];
                }
                prediction.frame++;
            }

            Debug.Assert(prediction.frame >= 0);

            // If we've made it this far, we must be predicting.  Go ahead and
            // forward the prediction frame contents.  Be sure to return the
            // frame number requested by the client, though.
            input = prediction;
            input.frame = requestedFrame;
            Log($"returning prediction frame number {input.frame} ({prediction.frame}).");

            return false;
        }

        public void AddInput(ref GameInput input)
        {
            int newFrame;

            Log($"adding input frame number {input.frame} to queue.");

            // These next two lines simply verify that inputs are passed in 
            // sequentially by the user, regardless of frame delay.
            Debug.Assert(lastUserAddedFrame == GameInput.NullFrame || input.frame == lastUserAddedFrame + 1);
            lastUserAddedFrame = input.frame;
               
            // Move the queue head to the correct point in preparation to
            // input the frame into the queue.
            newFrame = AdvanceQueueHead(input.frame);
            if (newFrame != GameInput.NullFrame)
            {
                AddDelayedInputToQueue(ref input, newFrame);
            }

            // Update the frame number for the input.  This will also set the
            // frame to GameInput::NullFrame for frames that get dropped (by
            // design).
            input.frame = newFrame;
        }

        public void AddDelayedInputToQueue(ref GameInput input, int frameNumber)
        {
            Log($"adding delayed input frame number {frameNumber} to queue.");

            Debug.Assert(input.size == prediction.size);
            Debug.Assert(lastAddedFrame == GameInput.NullFrame || frameNumber == lastAddedFrame + 1);
            Debug.Assert(frameNumber == 0 || inputs[PreviousFrame(head)].frame == frameNumber - 1);

            // Add the frame to the back of the queue
            inputs[head] = input;
            inputs[head].frame = frameNumber;
            head = (head + 1) % inputs.Length;
            length++;
            firstFrame = false;

            lastAddedFrame = frameNumber;

            if (prediction.frame != GameInput.NullFrame)
            {
                Debug.Assert(frameNumber == prediction.frame);

                // We've been predicting...  See if the inputs we've gotten match
                // what we've been predicting.  If so, don't worry about it.  If not,
                // remember the first input which was incorrect so we can report it
                // in GetFirstIncorrectFrame()
                if (firstIncorrectFrame == GameInput.NullFrame && !prediction.Equal(input, true))
                {
                    Log($"frame {frameNumber} does not match prediction.  marking error.");
                    firstIncorrectFrame = frameNumber;
                }

                // If this input is the same frame as the last one requested and we
                // still haven't found any mis-predicted inputs, we can dump out
                // of predition mode entirely!  Otherwise, advance the prediction frame
                // count up.
                if (prediction.frame == lastFrameRequested && firstIncorrectFrame == GameInput.NullFrame)
                {
                    Log("prediction is correct!  dumping out of prediction mode.");
                    prediction.frame = GameInput.NullFrame;
                }
                else
                {
                    prediction.frame++;
                }
            }
            Debug.Assert(length <= inputs.Length);
        }

        public int AdvanceQueueHead(int frame)
        {
            Log($"advancing queue head to frame {frame}.");

            int expectedFrame = firstFrame ? 0 : inputs[PreviousFrame(head)].frame + 1;

            frame += FrameDelay;

            if (expectedFrame > frame)
            {
                // This can occur when the frame delay has dropped since the last
                // time we shoved a frame into the system.  In this case, there's
                // no room on the queue.  Toss it.
                Log($"Dropping input frame {frame} (expected next frame to be {expectedFrame}).");
                return GameInput.NullFrame;
            }

            while (expectedFrame < frame)
            {
                // This can occur when the frame delay has been increased since the last
                // time we shoved a frame into the system.  We need to replicate the
                // last frame in the queue several times in order to fill the space
                // left.
                Log($"Adding padding frame {expectedFrame} to account for change in frame delay.");
                AddDelayedInputToQueue(ref inputs[PreviousFrame(head)], expectedFrame);
                expectedFrame++;
            }

            Debug.Assert(frame == 0 || frame == inputs[PreviousFrame(head)].frame + 1);
            return frame;
        }

        /// <summary>
        /// Helper method to log information from this class.
        /// </summary>
        /// <param name="msg">String message to output.</param>
        private void Log(string msg)
        {
            Logger.Log($"input q{id} | {msg}");
        }

        /// <summary>
        /// Retrieves the previous frame index from the inputs. If the offset is 0, retrieves the last
        /// index in the input queue.
        /// </summary>
        /// <param name="offset">Frame index to the queue.</param>
        /// <returns>The previous frame from the provided frame offset.</returns>
        private int PreviousFrame(int offset)
        {
            return (offset == 0) ? (inputs.Length - 1) : (offset - 1);
        }
    }
}
