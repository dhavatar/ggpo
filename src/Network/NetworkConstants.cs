﻿namespace GGPOSharp.Network
{
    public static class NetworkConstants
    {
        public const int NumSyncPackets = 5;
        public const int SyncRetryInterval = 2000;
        public const int SyncFirstRetryInterval = 500;
        public const int RunningRetryInterval = 200;
        public const int KeepAliveInterval = 200;
        public const int QualityReportInterval = 1000;
        public const int NetworkStatsInterval = 1000;
        public const int UpdShutdownTimer = 5000;
        public const int MaxSeqDistance = 1 << 15;
    }
}
