namespace GGPOSharp.Network
{
    /// <summary>
    /// The GGPONetworkStats function contains some statistics about the current session.
    /// </summary>
    public class GGPONetworkStats
    {
        /// <summary>
        /// The length of the queue containing UDP packets which have not yet been
        /// acknowledged by the end client. The length of the send queue is a rough
        /// indication of the quality of the connection. The longer the send queue,
        /// the higher the round-trip time between the clients. The send queue will
        /// also be longer than usual during high packet loss situations.
        /// </summary>
        public int sendQueueLenth;

        /// <summary>
        /// The number of inputs currently buffered by the GGPO.net network layer
        /// which have yet to be validated. The length of the prediction queue is
        /// roughly equal to the current frame number minus the frame number of
        /// the last packet in the remote queue.
        /// </summary>
        public int receiveQueueLength;

        /// <summary>
        /// The roundtrip packet transmission time as calcuated by GGPO.net.
        /// This will be roughly equal to the actual round trip packet
        /// transmission time + 2 the interval at which you call Idle
        /// or AdvanceFrame.
        /// </summary>
        public int ping;

        /// <summary>
        /// The estimated bandwidth used between the two clients, in kilobits per second.
        /// </summary>
        public int kbpsSent;

        /// <summary>
        /// The number of frames GGPO.net calculates that the local client is
        /// behind the remote client at this instant in time. For example, if
        /// at this instant the current game client is running frame 1002 and
        /// the remote game client is running frame 1009, this value will mostly
        /// likely roughly equal 7.
        /// </summary>
        public int localFramesBehind;

        /// <summary>
        /// The same as localFramesBehind, but calculated from the perspective of
        /// the remote player.
        /// </summary>
        public int remoteFramesBehind;
    }
}
