using GGPOSharp.Interfaces;
using System.Diagnostics;
using System.Threading;

namespace GGPOSharp
{
    public class Poll
    {
        int handleCount = 0;
        WaitHandle[] handles = new WaitHandle[Constants.MaxPollableHandles];

        StaticBuffer<IPollSink> loopSink = new StaticBuffer<IPollSink>(16);

        public Poll()
        {
            for (int i = 0; i < handles.Length; i++)
            {
                handles[i] = new AutoResetEvent(false);
            }
        }

        public void RegisterSink(IPollSink sink)
        {
            Debug.Assert(handleCount < Constants.MaxPollableHandles);

            loopSink.Push(sink);
            handleCount++;
        }

        public bool Pump(int timeout)
        {
            for (int i = 0; i < loopSink.Size; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(loopSink[i].OnLoopPoll), handles[i]);
            }

            return WaitHandle.WaitAny(handles, timeout) != WaitHandle.WaitTimeout;
        }

    }
}
