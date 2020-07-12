using GGPOSharp.Interfaces;

namespace GGPOSharp
{
    public class Poll
    {
        StaticBuffer<IPollSink> loopSink = new StaticBuffer<IPollSink>(16);

        public void RegisterSink(IPollSink sink)
        {
            loopSink.Push(sink);
        }

        public bool Pump(int timeout)
        {
            bool finished = false;

            for (int i = 0; i < loopSink.Size; i++)
            {
                finished = loopSink[i].OnLoopPoll() || finished;
            }

            return finished;
        }

    }
}
