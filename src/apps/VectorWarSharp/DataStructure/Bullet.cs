using System;

namespace VectorWar.DataStructure
{
    [Serializable]
    struct Bullet
    {
        public bool active;
        public Position position;
        public Velocity velocity;
    }
}
