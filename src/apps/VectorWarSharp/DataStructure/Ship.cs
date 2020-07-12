using System;

namespace VectorWar.DataStructure
{
    [Serializable]
    class Ship
    {
        public Position position;
        public Velocity velocity;
        public int radius;
        public double heading;
        public int health;
        public int speed;
        public int cooldown;
        public Bullet[] bullets = new Bullet[Constants.MaxBullets];
        public int score;
    }
}
