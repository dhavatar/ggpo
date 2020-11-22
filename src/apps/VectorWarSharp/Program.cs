using GGPOSharp;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace VectorWar
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Command line arguments for running the game.</param>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var app = new VectorWar();
            int offset = 0;
            int localPlayer = 0;

            // Window offsets for the different players
            Point[] windowOffsets = new Point[]
            {
                new Point(64, 64),
                new Point(740, 64),
                new Point(64, 600),
                new Point(700, 600),
            };

            if (args.Length < 3)
            {
                Syntax();
                return;
            }

            var localPort = int.Parse(args[offset++]);
            var numPlayers = int.Parse(args[offset++]);
            if (numPlayers < 0 || args.Length < offset + numPlayers)
            {
                Syntax();
                return;
            }

            if (args[offset] == "spectate")
            {
                string[] hostSplit = GetNetworkInfo(args[offset + 1]);
                var hostIp = hostSplit[0];
                var hostPort = int.Parse(hostSplit[1]);
                app.InitSpectator(localPort, numPlayers, hostIp, hostPort);
            }
            else
            {
                GGPOPlayer[] players = new GGPOPlayer[GGPOSharp.Constants.MaxSpectators + GGPOSharp.Constants.MaxPlayers];

                int i;
                for (i = 0; i < numPlayers; i++)
                {
                    string arg = args[offset++];

                    players[i].playerId = i + 1;
                    if (arg.Equals("local", StringComparison.InvariantCultureIgnoreCase))
                    {
                        players[i].type = GGPOPlayerType.Local;
                        localPlayer = i;
                        continue;
                    }

                    players[i].type = GGPOPlayerType.Remote;

                    string[] remoteSplit = GetNetworkInfo(arg);
                    players[i].ipAddress = remoteSplit[0];
                    players[i].port = int.Parse(remoteSplit[1]);
                }

                // Additional arguments past the number of players are spectators
                int numSpectators = 0;
                while (offset < args.Length)
                {
                    players[i].type = GGPOPlayerType.Spectator;

                    string[] remoteSplit = GetNetworkInfo(args[offset++]);
                    players[i].ipAddress = remoteSplit[0];
                    players[i].playerId = int.Parse(remoteSplit[1]);
                    i++;
                    numSpectators++;
                }

                if (localPlayer < windowOffsets.Length)
                {
                    app.Location = windowOffsets[localPlayer];
                }

                app.Init(localPort, numPlayers, players, numSpectators);
            }

            //app.SyncTest = true;
            Application.Run(app);
        }

        static void Syntax()
        {
            MessageBox.Show("Syntax: vectorwar.exe <local port> <num players> ('local' | <remote ip>:<remote port>)*",
              "Could not start");
        }

        static string[] GetNetworkInfo(string arg)
        {
            var split = arg.Split(':');
            if (split.Length != 2)
            {
                Syntax();
                Application.Exit();
            }

            return split;
        }
    }
}
