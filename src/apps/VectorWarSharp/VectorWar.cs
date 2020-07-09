using GGPOSharp;
using GGPOSharp.Backends;
using GGPOSharp.Interfaces;
using GGPOSharp.Logger;
using System;
using System.Buffers.Binary;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VectorWar
{
    public partial class VectorWar : Form, IGGPOSessionCallbacks
    {
        /// <summary>
        /// Set to true if performing a sync test.
        /// </summary>
        public bool SyncTest { get; set; } = false;

        private GameState gs;
        private NonGameState ngs = new NonGameState();
        private GGPOSession ggpo;
        private GdiRenderer renderer;

        public VectorWar()
        {
            InitializeComponent();

            Application.Idle += HandleApplicationIdle;
            Paint += VectorWar_Paint;

            renderer = new GdiRenderer(Bounds);
        }

        /// <summary>
        /// Initialize the vector war game.  This initializes the game state and
        /// the video renderer and creates a new network session.
        /// </summary>
        /// <param name="localPort"></param>
        /// <param name="numPlayers"></param>
        /// <param name="players"></param>
        /// <param name="numSpectators"></param>
        public void Init(int localPort, int numPlayers, GGPOPlayer[] players, int numSpectators)
        {
            // Initialize the game state
            gs = new GameState(Bounds, numPlayers);
            ngs.NumPlayers = numPlayers;

            ggpo = new PeerToPeerBackend(this, new ConsoleLogger(), localPort, numPlayers, 32);

            // automatically disconnect clients after 3000 ms and start our count-down timer
            // for disconnects after 1000 ms.   To completely disable disconnects, simply use
            // a value of 0 for ggpo_set_disconnect_timeout.
            ggpo.SetDisconnectTimeout(3000);
            ggpo.SetDisconnectNotifyStart(1000);

            for (int i = 0; i < numPlayers + numSpectators; i++)
            {
                ggpo.AddPlayer(players[i], out int playerHandle);
                ngs.players[i].playerHandle = playerHandle;
                ngs.players[i].type = players[i].type;
                if (players[i].type == GGPOPlayerType.Local)
                {
                    ngs.players[i].connectProgress = 100;
                    ngs.LocalPlayerHandle = playerHandle;
                    ngs.SetConnectState(playerHandle, PlayerConnectState.Connecting);
                    ggpo.SetFrameDelay(playerHandle, Constants.FrameDelay);
                }
                else
                {
                    ngs.players[i].connectProgress = 0;
                }
            }

            lblStatus.Text = "Connecting to peers.";
        }

        /// <summary>
        /// Create a new spectator session.
        /// </summary>
        /// <param name="localPort"></param>
        /// <param name="numPlayers"></param>
        /// <param name="hostIp"></param>
        /// <param name="hostPort"></param>
        public void InitSpectator(int localPort, int numPlayers, string hostIp, int hostPort)
        {
            // Initialize the game state
            gs = new GameState(Bounds, numPlayers);
            ngs.NumPlayers = numPlayers;

            // ggpo = new SpectatorBackend(this, new ConsoleLogger(), localPort, numPlayers, 32);

            lblStatus.Text = "Starting new spectator session";
        }

        /// <summary>
        /// Runs the game loop inside the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HandleApplicationIdle(object sender, EventArgs e)
        {
            while (IsApplicationIdle())
            {
                GameUpdate();
            }
        }

        /// <summary>
        /// Detects whether or not the form is still idle.
        /// </summary>
        /// <returns>True if the application is idle, false otherwise.</returns>
        bool IsApplicationIdle()
        {
            NativeMessage result;
            return PeekMessage(out result, IntPtr.Zero, (uint)0, (uint)0, (uint)0) == 0;
        }

        /// <summary>
        /// Updates any game state changes.
        /// </summary>
        void GameUpdate()
        {
            // ...
        }

        private void VectorWar_Paint(object sender, PaintEventArgs e)
        {
            renderer.Draw(e.Graphics, gs, ngs);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public Point Location;
        }

        [DllImport("user32.dll")]
        public static extern int PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);

        #region IGGPOSessionCallback Implementation

        public bool AdvanceFrame()
        {
            int[] inputs = new int[Constants.MaxShips];
            var inputBuffer = new byte[Constants.MaxShips * 4];
            var bufferSpan = new Span<byte>(inputBuffer);

            for (int i = 0; i < inputs.Length; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(bufferSpan.Slice(i * 4, 4), inputs[i]);
            }

            int disconnectFlags = 0;
            ggpo.SyncInput(Utility.GetByteArray(inputs), ref disconnectFlags);
            gs.Update(inputs, disconnectFlags);

            // update the checksums to display in the top of the window.  this
            // helps to detect desyncs.
            ngs.ChecksumNow = new NonGameState.ChecksumInfo
            {
                frameNumber = gs.FrameNumber,
                //checksum = Utility.CreateChecksum(Utility.)
            };

            if ((gs.FrameNumber % 90) == 0)
            {
                ngs.ChecksumPeriodic = ngs.ChecksumNow;
            }

            // Notify ggpo that we've moved forward exactly 1 frame.
            ggpo.AdvanceFrame();

            // Update the performance monitor display.
            int[] handles = new int[GGPOSharp.Constants.MaxPlayers];
            int count = 0;
            for (int i = 0; i < ngs.NumPlayers; i++)
            {
                if (ngs.players[i].type == GGPOPlayerType.Remote)
                {
                    handles[count++] = ngs.players[i].playerHandle;
                }
            }
            //ggpoutil_perfmon_update(ggpo, handles, count);

            return true;
        }

        public bool SaveGameState(ref Sync.SavedFrame frame)
        {
            throw new NotImplementedException();
        }

        public bool LoadGameState(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public bool LogGameState(string filename, byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public void OnConnected(int playerId)
        {
            throw new NotImplementedException();
        }

        public void OnConnectionInterrupted(int playerId, int disconnectTimeout)
        {
            throw new NotImplementedException();
        }

        public void OnConnectionResumed(int playerId)
        {
            throw new NotImplementedException();
        }

        public void OnDisconnected(int playerId)
        {
            throw new NotImplementedException();
        }

        public void OnRunning()
        {
            throw new NotImplementedException();
        }

        public void OnSynchronizing(int playerId, int count, int total)
        {
            throw new NotImplementedException();
        }

        public void OnSyncrhonized(int playerId)
        {
            throw new NotImplementedException();
        }

        public void OnTimeSync(int framesAhead)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
