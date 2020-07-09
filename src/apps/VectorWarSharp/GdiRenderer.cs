using GGPOSharp;
using System;
using System.Drawing;
using System.Windows.Forms;
using VectorWar.DataStructure;

namespace VectorWar
{
    /// <summary>
    /// Renderer that uses GDI to render the game state.
    /// </summary>
    class GdiRenderer
    {
        const int ProgressBarWidth = 100;
        const int ProgressBarTopOffset = 22;
        const int ProgressBarHeight = 8;
        const int ProgressTextOffset = (ProgressBarTopOffset - ProgressBarHeight + 4);

        TextFormatFlags[] alignments = new TextFormatFlags[]
        {
            TextFormatFlags.Top | TextFormatFlags.Left,
            TextFormatFlags.Top | TextFormatFlags.Right,
            TextFormatFlags.Bottom | TextFormatFlags.Left,
            TextFormatFlags.Bottom | TextFormatFlags.Right,
        };

        Color[] shipColors = new Color[4]
        {
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Gray,
        };

        SolidBrush redBrush = new SolidBrush(Color.Red);
        SolidBrush bulletBrush = new SolidBrush(Color.FromArgb(255, 192, 0));
        SolidBrush whiteBrush = new SolidBrush(Color.White);
        SolidBrush blackBrush = new SolidBrush(Color.Black);
        SolidBrush periodicTextBrush = new SolidBrush(Color.LightGray);
        SolidBrush nowTextBrush = new SolidBrush(Color.Gray);
        Font font = new Font("Tahoma", 12);
        Pen whitePen = new Pen(new SolidBrush(Color.White));
        Pen grayPen = new Pen(new SolidBrush(Color.Gray));

        Pen[] shipPens;
        Rectangle bounds;

        public GdiRenderer(Rectangle bounds)
        {
            this.bounds = bounds;

            shipPens = new Pen[4];
            for (int i = 0; i < 4; i++)
            {
                shipPens[i] = new Pen(new SolidBrush(shipColors[i]), 1);
            }
        }

        public void Draw(Graphics g, GameState gs, NonGameState ngs)
        {
            g.FillRectangle(blackBrush, bounds);
            g.DrawRectangle(whitePen, gs.Bounds);

            for (int i = 0; i < gs.NumberOfShips; i++)
            {
                DrawShip(g, i, gs);
                DrawConnectState(g, i, gs.Ships[i], ngs.players[i]);
            }

            RenderChecksum(g, 40, periodicTextBrush, ngs.ChecksumPeriodic);
            RenderChecksum(g, 56, nowTextBrush, ngs.ChecksumNow);
        }

        protected void RenderChecksum(Graphics g, int y, Brush brush, NonGameState.ChecksumInfo info)
        {
            g.DrawString($"Frame: {info.frameNumber:D04}  Checksum: {info.checksum:X08}",
                font,
                brush,
                new Point((bounds.Left + bounds.Right) / 2, bounds.Top + y));
        }

        protected void DrawShip(Graphics g, int which, GameState gs)
        {
            Ship ship = gs.Ships[which];
            Point[] shape = new Point[]
            {
                new Point(Constants.ShipRadius, 0),
                new Point(-Constants.ShipRadius,  Constants.ShipWidth),
                new Point(Constants.ShipTuck - Constants.ShipRadius, 0),
                new Point(-Constants.ShipRadius, -Constants.ShipWidth),
                new Point(Constants.ShipRadius, 0),
            };

            Point[] textOffsets = new Point[]
            {
                new Point(gs.Bounds.Left + 2, gs.Bounds.Top + 2),
                new Point(gs.Bounds.Right - 2, gs.Bounds.Top + 2),
                new Point(gs.Bounds.Left + 2, gs.Bounds.Bottom - 2),
                new Point(gs.Bounds.Right - 2, gs.Bounds.Bottom - 2),
            };

            for (int i = 0; i < shape.Length; i++)
            {
                double theta = ship.heading * Math.PI / 180;
                double cost = Math.Cos(theta);
                double sint = Math.Sin(theta);

                double newX = shape[i].X * cost - shape[i].Y * sint;
                double newY = shape[i].X * sint - shape[i].Y * cost;

                shape[i].X = (int)(newX + ship.position.x);
                shape[i].Y = (int)(newY + ship.position.y);
            }

            g.DrawPolygon(shipPens[which], shape);

            for (int i = 0; i < Constants.MaxBullets; i++)
            {
                if (ship.bullets[i].active)
                {
                    Rectangle bullet = new Rectangle(
                        (int)ship.bullets[i].position.x - 1,
                        (int)ship.bullets[i].position.y + 1,
                        2,
                        2
                        );
                    g.FillRectangle(bulletBrush, bullet);
                }
            }

            TextRenderer.DrawText(
                g,
                $"Hits: {ship.score}",
                font,
                textOffsets[which],
                shipColors[which],
                alignments[which]);
        }

        protected void DrawConnectState(Graphics g, int which, Ship ship, PlayerConnectionInfo info)
        {
            string status = string.Empty;
            int progress = -1;

            switch (info.state)
            {
                case PlayerConnectState.Connecting:
                    status = info.type == GGPOPlayerType.Local ? "Local Player" : "Connecting...";
                    break;

                case PlayerConnectState.Synchronizing:
                    progress = info.connectProgress;
                    status = info.type == GGPOPlayerType.Local ? "Local Player" : "Synchronizing...";
                    break;

                case PlayerConnectState.Disconnected:
                    status = "Disconnected";
                    break;

                case PlayerConnectState.Disconnecting:
                    status = "Waiting for player...";
                    progress = (int)(Utility.GetCurrentTime() - info.disconnectStart) * 100 / info.disconnectTimeout;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                TextRenderer.DrawText(
                    g,
                    status,
                    font,
                    new Point((int)ship.position.x, (int)ship.position.y + ProgressTextOffset),
                    shipColors[which],
                    TextFormatFlags.Top | TextFormatFlags.HorizontalCenter);
            }

            if (progress >= 0)
            {
                Brush bar = info.state == PlayerConnectState.Synchronizing ? whiteBrush : redBrush;
                Rectangle rc = new Rectangle(
                    (int)(ship.position.x - (ProgressBarWidth / 2)),
                    (int)(ship.position.y + ProgressBarTopOffset),
                    ProgressBarWidth,
                    ProgressBarHeight);

                g.DrawRectangle(grayPen, rc);
                rc.Width = Math.Min(100, progress) * ProgressBarWidth / 100;
                g.FillRectangle(bar, Rectangle.Inflate(rc, -1, -1));
            }
        }
    }
}
