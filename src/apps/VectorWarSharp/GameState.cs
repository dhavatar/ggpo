using GGPOSharp.Interfaces;
using GGPOSharp.Logger;
using System;
using System.Drawing;
using VectorWar.DataStructure;

namespace VectorWar
{
    /// <summary>
    /// Encapsulates all the game state for the vector war application inside
    /// a single structure. This makes it trivial to implement our GGPO
    /// save and load functions.
    /// </summary>
    class GameState : IGameState
    {
        private static readonly ILog logger = new ConsoleLogger();

        public int FrameNumber { get; set; } = 0;

        public Rectangle Bounds { get; private set; }

        public int NumberOfShips { get; private set; }

        public Ship[] Ships { get; private set; } = new Ship[Constants.MaxShips];

        public GameState(Rectangle bounds, int numPlayers)
        {
            Bounds = Rectangle.Inflate(bounds, -8, -8);

            int width = Bounds.Right - Bounds.Left;
            int height = Bounds.Bottom - Bounds.Top;
            int r = height / 4;

            NumberOfShips = numPlayers;
            for (int i = 0; i < NumberOfShips; i++)
            {
                int heading = i * 360 / numPlayers;
                double theta = heading * Math.PI / 180;
                double cost = Math.Cos(theta);
                double sint = Math.Sin(theta);

                Ships[i].position.x = (width / 2) + r * cost;
                Ships[i].position.y = (height / 2) + r * sint;
                Ships[i].heading = (heading + 180) % 360;
                Ships[i].health = Constants.StartingHealth;
                Ships[i].radius = Constants.ShipRadius;
            }

            Bounds = Rectangle.Inflate(Bounds, -8, -8);
        }

        public void GetShipAI(int index, ref double heading, ref double thrust, ref bool fire)
        {
            heading = (Ships[index].heading + 5) % 360;
            thrust = 0;
            fire = false;
        }

        public void ParseShipInputs(int inputs, int index, ref double heading, ref double thrust, ref bool fire)
        {
            logger.Log($"parsing ship {index} inputs: {inputs}.");

            if ((inputs & (int)VectorWarInputs.RotateRight) > 0)
            {
                heading = (Ships[index].heading + Constants.RotateIncrement) % 360;
            }
            else if ((inputs & (int)VectorWarInputs.RotateLeft) > 0)
            {
                heading = (Ships[index].heading - Constants.RotateIncrement + 360) % 360;
            }
            else
            {
                heading = Ships[index].heading;
            }

            if ((inputs & (int)VectorWarInputs.Thrust) > 0)
            {
                thrust = Constants.ShipThrust;
            }
            else if ((inputs & (int)VectorWarInputs.Brake) > 0)
            {
                thrust = -Constants.ShipThrust;
            }
            else
            {
                thrust = 0;
            }

            fire = (inputs & (int)VectorWarInputs.Fire) > 0;
        }

        public void MoveShip(int index, double heading, double thrust, bool fire)
        {
            logger.Log($"calculation of new ship coordinates: (thrust:{thrust:F0.4} heading:{heading:F0.4}).");

            var ship = Ships[index];
            ship.heading = heading;

            if (ship.cooldown == 0 && fire)
            {
                logger.Log("firing bullet.");
                for (int i = 0; i < Constants.MaxBullets; i++)
                {
                    double dx = Math.Cos(DegToRad(ship.heading));
                    double dy = Math.Sin(DegToRad(ship.heading));
                    if (!ship.bullets[i].active)
                    {
                        ship.bullets[i].active = true;
                        ship.bullets[i].position.x = ship.position.x + (ship.radius * dx);
                        ship.bullets[i].position.y = ship.position.y + (ship.radius * dy);
                        ship.bullets[i].velocity.dx = ship.velocity.dx + (Constants.BulletSpeed * dx);
                        ship.bullets[i].velocity.dy = ship.velocity.dy + (Constants.BulletSpeed * dy);
                        ship.cooldown = Constants.BulletCooldown;
                        break;
                    }
                }
            }

            if (thrust > 0)
            {
                double dx = thrust * Math.Cos(DegToRad(heading));
                double dy = thrust * Math.Sin(DegToRad(heading));

                ship.velocity.dx += dx;
                ship.velocity.dy += dy;
                double mag = Math.Sqrt(ship.velocity.dx * ship.velocity.dx +
                                 ship.velocity.dy * ship.velocity.dy);
                if (mag > Constants.ShipMaxThrust)
                {
                    ship.velocity.dx = (ship.velocity.dx * Constants.ShipMaxThrust) / mag;
                    ship.velocity.dy = (ship.velocity.dy * Constants.ShipMaxThrust) / mag;
                }
            }

            logger.Log($"new ship velocity: (dx:{ship.velocity.dx:F0.4} dy:{ship.velocity.dy:F2}).");

            ship.position.x += ship.velocity.dx;
            ship.position.y += ship.velocity.dy;
            logger.Log($"new ship position: (dx:{ship.velocity.dx:F0.4} dy:{ship.velocity.dy:F2}).");

            if (ship.position.x - ship.radius < Bounds.Left ||
                ship.position.x + ship.radius > Bounds.Right)
            {
                ship.velocity.dx *= -1;
                ship.position.x += (ship.velocity.dx * 2);
            }
            if (ship.position.y - ship.radius < Bounds.Top ||
                ship.position.y + ship.radius > Bounds.Bottom)
            {
                ship.velocity.dy *= -1;
                ship.position.y += (ship.velocity.dy * 2);
            }
            for (int i = 0; i < Constants.MaxBullets; i++)
            {
                if (ship.bullets[i].active)
                {
                    ship.bullets[i].position.x += ship.bullets[i].velocity.dx;
                    ship.bullets[i].position.y += ship.bullets[i].velocity.dy;
                    if (ship.bullets[i].position.x < Bounds.Left ||
                        ship.bullets[i].position.y < Bounds.Top ||
                        ship.bullets[i].position.x > Bounds.Right ||
                        ship.bullets[i].position.y > Bounds.Bottom)
                    {
                        ship.bullets[i].active = false;
                    }
                    else
                    {
                        for (int j = 0; j < NumberOfShips; j++)
                        {
                            Ship other = Ships[j];
                            if (Distance(ship.bullets[i].position, other.position) < other.radius)
                            {
                                ship.score++;
                                other.health -= Constants.BulletDamage;
                                ship.bullets[i].active = false;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Update(int[] inputs, int disconnectFlags)
        {
            FrameNumber++;
            for (int i = 0; i < NumberOfShips; i++)
            {
                double thrust = 0;
                double heading = 0;
                bool fire = false;

                if ((disconnectFlags & (1 << i)) > 0)
                {
                    GetShipAI(i, ref heading, ref thrust, ref fire);
                }
                else
                {
                    ParseShipInputs(inputs[i], i, ref heading, ref thrust, ref fire);
                }
                MoveShip(i, heading, thrust, fire);

                if (Ships[i].cooldown > 0)
                {
                    Ships[i].cooldown--;
                }
            }
        }

        private double DegToRad(double deg)
        {
            return Math.PI * deg / 180;
        }

        private double Distance(in Position lhs, in Position rhs)
        {
            var x = rhs.x - lhs.y;
            var y = rhs.y - lhs.y;
            return Math.Sqrt(x * x + y * y);
        }
    }
}
