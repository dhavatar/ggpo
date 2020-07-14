using GGPOSharp;
using GGPOSharp.Backends;
using GGPOSharp.Interfaces;
using GGPOSharp.Logger;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VectorWar.DataStructure;

namespace VectorWar
{
    public partial class VectorWar : Form, IGGPOSessionCallbacks
    {
        static readonly Dictionary<Keys, int> InputTable = new Dictionary<Keys, int>()
        {
            { Keys.Up, (int)VectorWarInputs.Thrust },
            { Keys.Down, (int)VectorWarInputs.Brake },
            { Keys.Left, (int)VectorWarInputs.RotateLeft },
            { Keys.Right, (int)VectorWarInputs.RotateRight },
            { Keys.D, (int)VectorWarInputs.Fire },
            { Keys.S, (int)VectorWarInputs.Bomb },
        };

        /// <summary>
        /// Set to true if performing a sync test.
        /// </summary>
        public bool SyncTest { get; set; } = false;

        public int LocalInputs { get; set; } = 0;

        private GameState gs;
        private NonGameState ngs = new NonGameState();
        private GGPOSession ggpo;
        private GdiRenderer renderer;
        private PerformanceMonitor monitor = new PerformanceMonitor();

        private long next = 0;
        private long now = Utility.GetCurrentTime();

        private Random random = new Random();

        public VectorWar()
        {
            InitializeComponent();
            monitor.Hide();

            // Double buffer and other parameters to help make the drawing have less flicker
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);

            renderer = new GdiRenderer(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));

            Application.Idle += HandleApplicationIdle;
            Paint += VectorWar_Paint;
            KeyDown += VectorWar_KeyDown;
            KeyUp += VectorWar_KeyUp;
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
            InitializeGameState(numPlayers);

            ggpo = new PeerToPeerBackend(this, new ConsoleLogger(), localPort, numPlayers, 4);

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
            InitializeGameState(numPlayers);

            ggpo = new SpectatorBackend(this, new ConsoleLogger(), localPort, numPlayers, 4, hostIp, hostPort);

            lblStatus.Text = "Starting new spectator session";
        }

        private void InitializeGameState(int numPlayers)
        {   
            gs = new GameState(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), numPlayers);
            ngs.NumPlayers = numPlayers;
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
                now = Utility.GetCurrentTime();
                ggpo.Idle((int)Math.Max(0, next - now - 1));
                if (now >= next)
                {
                    GameUpdate();
                    Refresh();
                    next = now + (long)(1000 / 60f);
                }
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
            var result = GGPOErrorCode.OK;
            int disconnectFlags = 0;
            int[] inputs = new int[Constants.MaxShips];

            if (ngs.LocalPlayerHandle != GGPOSharp.Constants.InvalidHandle)
            {
                if (SyncTest)
                {
                    LocalInputs = random.Next();
                }

                result = ggpo.AddLocalInput(ngs.LocalPlayerHandle, BitConverter.GetBytes(LocalInputs));
            }

            if (result == GGPOErrorCode.Success)
            {
                var inputBuffer = new byte[Constants.MaxShips * 4];
                var bufferSpan = new Span<byte>(inputBuffer);

                ggpo.SyncInput(ref inputBuffer, ref disconnectFlags);
                if (result == GGPOErrorCode.Success)
                {
                    // Convert the byte buffer back into an int array
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        inputs[i] = BinaryPrimitives.ReadInt32LittleEndian(bufferSpan.Slice(i * 4, 4));
                    }

                    UpdateFrame(inputs, disconnectFlags);
                }
            }
        }

        private void UpdateFrame(int[] inputs, int disconnectFlags)
        {
            gs.Update(inputs, disconnectFlags);

            // update the checksums to display in the top of the window.  this
            // helps to detect desyncs.
            ngs.ChecksumNow = new NonGameState.ChecksumInfo
            {
                frameNumber = gs.FrameNumber,
                checksum = Utility.CreateChecksum(Utility.GetByteArray(gs)),
            };

            if ((gs.FrameNumber % 90) == 0)
            {
                ngs.ChecksumPeriodic = ngs.ChecksumNow;
            }

            // Notify ggpo that we've moved forward exactly 1 frame.
            ggpo.AdvanceFrame();

            // Update the performance monitor display.
            int[] handles = new int[Constants.MaxPlayers];
            int count = 0;
            for (int i = 0; i < ngs.NumPlayers; i++)
            {
                if (ngs.players[i].type == GGPOPlayerType.Remote)
                {
                    handles[count++] = ngs.players[i].playerHandle;
                }
            }

            monitor.Update(ggpo, handles, count);
        }

        public void DisconnectPlayer(int player)
        {
            if (player < ngs.NumPlayers)
            {
                string statusMsg = string.Empty;
                GGPOErrorCode result = ggpo.DisconnectPlayer(player);
                if (result == GGPOErrorCode.Success)
                {
                    statusMsg = $"Disconnected player {player}.";
                }
                else
                {
                    statusMsg = $"Error while disconnecting player (err:{result}).";
                }

                lblStatus.Text = statusMsg;
            }
        }

        private void VectorWar_KeyUp(object sender, KeyEventArgs e)
        {
            if (InputTable.ContainsKey(e.KeyCode))
            {
                LocalInputs &= ~InputTable[e.KeyCode];
            }
        }

        private void VectorWar_KeyDown(object sender, KeyEventArgs e)
        {
            if (InputTable.ContainsKey(e.KeyCode))
            {
                LocalInputs |= InputTable[e.KeyCode];
            }
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

        /// <summary>
        /// Notification from GGPO we should step foward exactly 1 frame
        /// during a rollback.
        /// </summary>
        /// <returns></returns>
        public bool AdvanceFrame()
        {
            int[] inputs = new int[Constants.MaxShips];
            int disconnectFlags = 0;

            var inputBuffer = new byte[Constants.MaxShips * 4];
            var bufferSpan = new Span<byte>(inputBuffer);

            // Make sure we fetch new inputs from GGPO and use those to update
            // the game state instead of reading from the keyboard.
            ggpo.SyncInput(ref inputBuffer, ref disconnectFlags);

            // Convert the byte buffer back into an int array
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = BinaryPrimitives.ReadInt32LittleEndian(bufferSpan.Slice(i * 4, 4));
            }

            UpdateFrame(inputs, disconnectFlags);
            return true;
        }

        /// <summary>
        /// Makes our current state match the state passed in by GGPO.
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public bool SaveGameState(ref Sync.SavedFrame frame)
        {
            frame.buffer = gs;
            frame.frame = gs.FrameNumber;
            frame.checksum = Utility.CreateChecksum(Utility.GetByteArray(gs));
            return true;
        }

        /// <summary>
        /// Save the current state to a buffer and return it to GGPO via the
        /// buffer and len parameters.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool LoadGameState(IGameState state)
        {
            gs = state as GameState;
            return true;
        }

        /// <summary>
        /// Log the gamestate. Used by the synctest debugging tool.
        /// </summary>
        /// <param name="filename">Filename to write.</param>
        /// <param name="state"><see cref="IGameState"/> object containing the current game state.</param>
        /// <returns>True.</returns>
        public bool LogGameState(string filename, IGameState state)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
            {
                var gameState = state as GameState;
                file.WriteLine("GameState object.");
                file.WriteLine($"  bounds: {gameState.Bounds.Left},{gameState.Bounds.Top} x {gameState.Bounds.Right},{gameState.Bounds.Bottom}.");
                file.WriteLine($"  numShips: {gameState.NumberOfShips}.");

                for (int i = 0; i < gameState.NumberOfShips; i++)
                {
                    Ship ship = gameState.Ships[i];
                    file.WriteLine($"  ship {i} position:  {ship.position:F4}");
                    file.WriteLine($"  ship {i} velocity:  {ship.velocity:F4}");
                    file.WriteLine($"  ship {i} radius:    {ship.radius}.");
                    file.WriteLine($"  ship {i} heading:   {ship.heading}.");
                    file.WriteLine($"  ship {i} health:    {ship.health}.");
                    file.WriteLine($"  ship {i} speed:     {ship.speed}.");
                    file.WriteLine($"  ship {i} cooldown:  {ship.cooldown}.");
                    file.WriteLine($"  ship {i} score:     {ship.score}.");

                    for (int j = 0; j < Constants.MaxBullets; j++)
                    {
                        file.WriteLine($"  ship {i} bullet {j}: {ship.bullets[j].position:F2} -> {ship.bullets[j].velocity:F2}");
                    }
                }
            }

            return true;
        }

        public void OnConnected(int playerId)
        {
            ngs.SetConnectState(playerId, PlayerConnectState.Synchronizing);
        }

        public void OnConnectionInterrupted(int playerId, int disconnectTimeout)
        {
            ngs.SetDisconnectTimeout(playerId, Utility.GetCurrentTime(), disconnectTimeout);
        }

        public void OnConnectionResumed(int playerId)
        {
            ngs.SetConnectState(playerId, PlayerConnectState.Running);
        }

        public void OnDisconnected(int playerId)
        {
            ngs.SetConnectState(playerId, PlayerConnectState.Disconnected);
        }

        public void OnRunning()
        {
            ngs.SetConnectState(PlayerConnectState.Running);
            lblStatus.Text = string.Empty;
        }

        public void OnSynchronizing(int playerId, int count, int total)
        {
            int progress = (int)(100 * count / (float)total);
            ngs.UpdateConnectProgress(playerId, progress);
        }

        public void OnSyncrhonized(int playerId)
        {
            ngs.UpdateConnectProgress(playerId, 100);
        }

        public void OnTimeSync(int framesAhead)
        {
            // Thread.Sleep(1000 * framesAhead / 60);
        }

        #endregion
    }
}
