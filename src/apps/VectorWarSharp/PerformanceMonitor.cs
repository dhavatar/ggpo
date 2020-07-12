using GGPOSharp;
using GGPOSharp.Network;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace VectorWar
{
    public partial class PerformanceMonitor : Form
    {
        const int MaxGraphSize = 4096;
        const int MaxFairness = 20;

        public int GraphSize { get; set; }

        SolidBrush blackBrush = new SolidBrush(Color.Black);
        Pen greenPen = new Pen(new SolidBrush(Color.Green));
        Pen redPen = new Pen(new SolidBrush(Color.Red));
        Pen bluePen = new Pen(new SolidBrush(Color.Blue));
        Pen yellowPen = new Pen(new SolidBrush(Color.Yellow));
        Pen grayPen = new Pen(new SolidBrush(Color.Gray));
        Pen pinkPen = new Pen(new SolidBrush(Color.Pink));

        Pen[] fairnessPens;

        int numPlayers = 0;
        int firstGraphIndex = 0;
        int graphSize = 0;

        int[][] pingGraph = new int[GGPOSharp.Constants.MaxPlayers][]
            {
                new int[MaxGraphSize], new int[MaxGraphSize],
                new int[MaxGraphSize], new int[MaxGraphSize],
            };
        int[][] localFairnessGraph = new int[GGPOSharp.Constants.MaxPlayers][]
            {
                new int[MaxGraphSize], new int[MaxGraphSize],
                new int[MaxGraphSize], new int[MaxGraphSize],
            };
        int[][] remoteFairnessGraph = new int[GGPOSharp.Constants.MaxPlayers][]
            {
                new int[MaxGraphSize], new int[MaxGraphSize],
                new int[MaxGraphSize], new int[MaxGraphSize],
            };

        int[] fairnessGraph = new int[MaxGraphSize];
        int[] predictQueueGraph = new int[MaxGraphSize];
        int[] remoteQueueGraph = new int[MaxGraphSize];
        int[] sendQueueGraph = new int[MaxGraphSize];

        long lastTextUpdateTime = 0;

        public PerformanceMonitor()
        {
            InitializeComponent();

            fairnessPens = new Pen[] { bluePen, grayPen, redPen, pinkPen };
            lblPid.Text = $"{Process.GetCurrentProcess().Id}";
        }

        public void Update(GGPOSession ggpo, int[] players, int numPlayers)
        {
            var stats = new GGPONetworkStats();
            int i = 0;

            this.numPlayers = numPlayers;

            if (graphSize < MaxGraphSize)
            {
                i = graphSize++;
            }
            else
            {
                i = firstGraphIndex;
                firstGraphIndex = (firstGraphIndex + 1) % MaxGraphSize;
            }

            for (int j = 0; j < numPlayers; j++)
            {
                ggpo.GetNetworkStats(players[j], out stats);

                // Ping
                pingGraph[j][i] = (int)stats.Ping;

                // Frame Advantage
                localFairnessGraph[j][i] = stats.LocalFramesBehind;
                remoteFairnessGraph[j][i] = stats.RemoteFramesBehind;
                if (stats.LocalFramesBehind < 0 && stats.RemoteFramesBehind < 0)
                {
                    // Both think it's unfair (which, ironically, is fair).  Scale both and subtrace.
                    fairnessGraph[i] = Math.Abs(Math.Abs(stats.LocalFramesBehind) - Math.Abs(stats.RemoteFramesBehind));
                }
                else if (stats.LocalFramesBehind > 0 && stats.RemoteFramesBehind > 0)
                {
                    // Impossible!  Unless the network has negative transmit time.  Odd....
                    fairnessGraph[i] = 0;
                }
                else
                {
                    // They disagree.  Add.
                    fairnessGraph[i] = Math.Abs(stats.LocalFramesBehind) + Math.Abs(stats.RemoteFramesBehind);
                }
            }

            long now = Utility.GetCurrentTime();
            if (now > lastTextUpdateTime + 500)
            {
                lblNetworkLag.Text = $"{stats.Ping} ms";
                lblFrameLag.Text = $"{stats.Ping * 60 / 1000f:F1}";
                lblBandwidth.Text = $"{stats.KbpsSent / 8f:F2} kilobytes/sec";
                lblLocalAhead.Text = $"{stats.LocalFramesBehind} frames";
                lblRemoteAhead.Text = $"{stats.RemoteFramesBehind} frames";

                lastTextUpdateTime = now;
            }
        }

        private void networkGraph_Paint(object sender, PaintEventArgs e)
        {
            DrawGrid(e.Graphics);

            for (int i = 0; i < numPlayers; i++)
            {
                DrawGraph(e.Graphics, greenPen, pingGraph[i], 0, 500);
            }

            DrawGraph(e.Graphics, pinkPen, predictQueueGraph, 0, 14);
            DrawGraph(e.Graphics, redPen, remoteQueueGraph, 0, 14);
            DrawGraph(e.Graphics, bluePen, sendQueueGraph, 0, 14);
        }

        private void fairnessGraph_Paint(object sender, PaintEventArgs e)
        {
            int midpoint = (int)(e.Graphics.VisibleClipBounds.Height / 2);

            DrawGrid(e.Graphics);
            e.Graphics.DrawLine(
                grayPen,
                new Point((int)e.Graphics.VisibleClipBounds.Left, midpoint),
                new Point((int)e.Graphics.VisibleClipBounds.Right, midpoint));

            for (int i = 0; i < numPlayers; i++)
            {
                DrawGraph(e.Graphics, fairnessPens[i], remoteFairnessGraph[i], -MaxFairness, MaxFairness);
            }
            DrawGraph(e.Graphics, yellowPen, fairnessGraph, -MaxFairness, MaxFairness);
        }

        private void btnClose_Click(object sender, System.EventArgs e)
        {
            Hide();
        }

        private void DrawGraph(Graphics g, Pen pen, int[] graph, int min, int max)
        {
            Point[] pt = new Point[MaxGraphSize];
            int height = (int)(g.VisibleClipBounds.Height);
            int width = (int)(g.VisibleClipBounds.Width);
            int range = max - min;
            int offset = 0;
            int count = graph.Length;

            if (count > width)
            {
                offset = count - width;
                count = width;
            }

            for (int i = 0; i < count; i++)
            {
                int value = graph[(firstGraphIndex + offset + i) % MaxGraphSize] - min;
                int y = height - (value * height / range);
                pt[i].X = (width - count) + i;
                pt[i].Y = y;
            }

            g.DrawPolygon(pen, pt);
        }

        private void DrawGrid(Graphics g)
        {
            g.FillRectangle(blackBrush, g.VisibleClipBounds);
        }
    }
}
